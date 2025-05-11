class Edge
{
    public int To { get; set; }
    public int Dist { get; set; }
    public int RequiredMask { get; set; }
    public int KeyBit { get; set; }
}

class Node
{
    public int Robots { get; }
    public int KeysMask { get; }

    public Node(int robots, int keysMask)
    {
        Robots = robots;
        KeysMask = keysMask;
    }
}

class MinHeap
{
    private readonly List<Tuple<int, Node>> data = new List<Tuple<int, Node>>();

    public int Count => data.Count;

    public void Push(int cost, Node node)
    {
        data.Add(Tuple.Create(cost, node));
        var i = data.Count - 1;

        while (i > 0)
        {
            var parent = (i - 1) >> 1;

            if (data[parent].Item1 <= data[i].Item1)
                break;

            var temp = data[i];
            data[i] = data[parent];
            data[parent] = temp;
            i = parent;
        }
    }

    public Tuple<int, Node> Pop()
    {
        var result = data[0];
        var last = data[data.Count - 1];
        data.RemoveAt(data.Count - 1);

        if (data.Count > 0)
        {
            data[0] = last;
            var i = 0;

            while (true)
            {
                var left = (i << 1) + 1;
                var right = left + 1;
                var smallest = i;

                if (left < data.Count && data[left].Item1 < data[smallest].Item1)
                    smallest = left;

                if (right < data.Count && data[right].Item1 < data[smallest].Item1)
                    smallest = right;

                if (smallest == i)
                    break;

                var temp = data[i];
                data[i] = data[smallest];
                data[smallest] = temp;
                i = smallest;
            }
        }

        return result;
    }
}

class CompressedGraph
{
    public List<Edge>[] Edges { get; }
    public int TotalVertices { get; }
    public int AllKeysMask { get; }
    public Dictionary<char, int> KeyToVertex { get; }

    public CompressedGraph(List<Edge>[] edges, int totalVertices, int allKeysMask, Dictionary<char, int> keyToVertex)
    {
        Edges = edges;
        TotalVertices = totalVertices;
        AllKeysMask = allKeysMask;
        KeyToVertex = keyToVertex;
    }
}

class Program
{
    private static readonly (int dr, int dc)[] directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    private static CompressedGraph BuildCompressedGraph(List<List<char>> grid)
    {
        var rows = grid.Count;
        var cols = grid[0].Count;
        var starts = new List<Tuple<int, int>>();
        var keyPositions = new Dictionary<char, Tuple<int, int>>();

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = grid[r][c];

                if (cell == '@')
                {
                    starts.Add(Tuple.Create(r, c));
                    grid[r][c] = '.';
                }
                else if (cell >= 'a' && cell <= 'z')
                {
                    keyPositions[cell] = Tuple.Create(r, c);
                }
            }
        }

        var startCount = starts.Count;
        var keyCount = keyPositions.Count;
        var totalVertices = startCount + keyCount;
        var vertices = new Tuple<int, int>[totalVertices];

        for (var i = 0; i < startCount; i++)
            vertices[i] = starts[i];

        var keyToVertex = new Dictionary<char, int>();
        var keyIndex = 0;

        foreach (var kvp in keyPositions)
        {
            var vertexId = startCount + keyIndex++;
            keyToVertex[kvp.Key] = vertexId;
            vertices[vertexId] = kvp.Value;
        }

        var edges = new List<Edge>[totalVertices];

        for (var i = 0; i < totalVertices; i++)
        {
            edges[i] = new List<Edge>();
            BuildEdges(i, vertices[i], grid, edges, keyToVertex);
        }

        var allKeysMask = 0;
        foreach (var key in keyPositions.Keys)
            allKeysMask |= 1 << (key - 'a');

        return new CompressedGraph(edges, totalVertices, allKeysMask, keyToVertex);
    }

    private static void BuildEdges(int fromIdx, Tuple<int, int> start, List<List<char>> grid, List<Edge>[] edges, Dictionary<char, int> keyToVertex)
    {
        var rows = grid.Count;
        var cols = grid[0].Count;
        var visited = new bool[rows, cols];
        var queue = new Queue<Tuple<int, int, int, int>>();

        queue.Enqueue(Tuple.Create(start.Item1, start.Item2, 0, 0));
        visited[start.Item1, start.Item2] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var r = current.Item1;
            var c = current.Item2;
            var dist = current.Item3;
            var mask = current.Item4;

            for (var d = 0; d < directions.Length; d++)
            {
                var nr = r + directions[d].dr;
                var nc = c + directions[d].dc;

                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols)
                    continue;

                var cell = grid[nr][nc];

                if (cell == '#')
                    continue;

                var nextMask = mask;

                if (cell >= 'A' && cell <= 'Z')
                    nextMask |= 1 << (cell - 'A');

                if (!visited[nr, nc])
                {
                    visited[nr, nc] = true;
                    queue.Enqueue(Tuple.Create(nr, nc, dist + 1, nextMask));
                }

                if (cell >= 'a' && cell <= 'z')
                {
                    var toIdx = keyToVertex[cell];
                    var alreadyExists = false;

                    foreach (var e in edges[fromIdx])
                    {
                        if (e.To == toIdx)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        edges[fromIdx].Add(new Edge
                        {
                            To = toIdx,
                            Dist = dist + 1,
                            RequiredMask = nextMask,
                            KeyBit = 1 << (cell - 'a')
                        });
                    }
                }
            }
        }
    }

    private static int EncodeRobots(int[] positions) =>
        positions[0] | (positions[1] << 5) | (positions[2] << 10) | (positions[3] << 15);

    private static void DecodeRobots(int code, int[] output)
    {
        output[0] = code & 31;
        output[1] = (code >> 5) & 31;
        output[2] = (code >> 10) & 31;
        output[3] = (code >> 15) & 31;
    }

    private static ulong MakeStateKey(int robot, int keysMask) =>
        ((ulong)robot << 26) | (uint)keysMask;

    static int Solve(List<List<char>> data)
    {
        var graph = BuildCompressedGraph(data);

        var startPositions = new int[] { 0, 1, 2, 3 };
        var initCode = EncodeRobots(startPositions);
        var startNode = new Node(initCode, 0);

        var best = new Dictionary<ulong, int>();
        var priorityQueue = new MinHeap();
        var robotsPos = new int[4];

        priorityQueue.Push(0, startNode);
        best[MakeStateKey(initCode, 0)] = 0;

        while (priorityQueue.Count > 0)
        {
            var currentEntry = priorityQueue.Pop();
            var currentDist = currentEntry.Item1;
            var currentNode = currentEntry.Item2;
            var currentKey = MakeStateKey(currentNode.Robots, currentNode.KeysMask);

            if (best[currentKey] < currentDist)
                continue;

            if (currentNode.KeysMask == graph.AllKeysMask)
                return currentDist;

            DecodeRobots(currentNode.Robots, robotsPos);

            for (var i = 0; i < 4; i++)
            {
                var currentPos = robotsPos[i];

                foreach (var edge in graph.Edges[currentPos])
                {
                    if ((currentNode.KeysMask & edge.KeyBit) != 0)
                        continue;

                    if ((edge.RequiredMask & ~currentNode.KeysMask) != 0)
                        continue;

                    var newPositions = (int[])robotsPos.Clone();
                    newPositions[i] = edge.To;
                    Array.Sort(newPositions);

                    var newCode = EncodeRobots(newPositions);
                    var newKeys = currentNode.KeysMask | edge.KeyBit;
                    var newStateKey = MakeStateKey(newCode, newKeys);
                    var newDist = currentDist + edge.Dist;

                    if (!best.TryGetValue(newStateKey, out var existingDist) || newDist < existingDist)
                    {
                        best[newStateKey] = newDist;
                        priorityQueue.Push(newDist, new Node(newCode, newKeys));
                    }
                }
            }
        }

        return -1;
    }

    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;

        while ((line = Console.ReadLine()) != null && line.Length > 0)
        {
            data.Add(line.ToCharArray().ToList());
        }

        return data;
    }

    static void Main()
    {
        var data = GetInput();
        var result = Solve(data);

        if (result == -1)
            Console.WriteLine("No solution found");
        else
            Console.WriteLine(result);
    }
}