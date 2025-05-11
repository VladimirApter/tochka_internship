using System.Text.RegularExpressions;

class HotelCapacity
{
    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var occupancyDeltaByDate = new Dictionary<DateTime, int>();
        foreach (var guest in guests)
        {
            var checkIn = DateTime.Parse(guest.CheckIn);
            var checkOut = DateTime.Parse(guest.CheckOut);

            occupancyDeltaByDate.TryAdd(checkIn, 0);
            occupancyDeltaByDate.TryAdd(checkOut, 0);

            occupancyDeltaByDate[checkIn]++;
            occupancyDeltaByDate[checkOut]--;
        }

        var capacity = 0;
        foreach (var kvp in occupancyDeltaByDate.OrderBy(x => x.Key))
        {
            capacity += kvp.Value;
            if (capacity > maxCapacity)
                return false;
        }

        return true;
    }

    class Guest
    {
        public string Name { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }

    static void Main()
    {
        int maxCapacity;
        int n;

        if (!int.TryParse(Console.ReadLine(), out maxCapacity) || !int.TryParse(Console.ReadLine(), out n))
        {
            Console.WriteLine("Invalid input");
            return;
        }

        var guests = new List<Guest>();

        for (var i = 0; i < n; i++)
        {
            var line = Console.ReadLine();
            var guest = ParseGuest(line);
            guests.Add(guest);
        }

        var result = CheckCapacity(maxCapacity, guests);
        Console.WriteLine(result ? "True" : "False");
    }

    // Simple JSON parser for Guest object
    static Guest ParseGuest(string json)
    {
        var guest = new Guest();

        var nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;

        var checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;

        var checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;

        return guest;
    }
}
