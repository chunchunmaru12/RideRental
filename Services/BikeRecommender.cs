namespace RideRental.Services
{
    using RideRental.Models;
    using System;

    public static class BikeRecommender
    {
        // Main similarity calculator using mixed features
        public static double CosineSimilarity(Bike a, Bike b)
        {
            var aVec = new[]
            {
                ParsePower(a.Power),
                HashString(a.Category),
                HashString(a.EngineType),
                HashString(a.ColorOptions),
                HashString(a.Model)
            };

            var bVec = new[]
            {
                ParsePower(b.Power),
                HashString(b.Category),
                HashString(b.EngineType),
                HashString(b.ColorOptions),
                HashString(b.Model)
            };

            double dot = 0, aMag = 0, bMag = 0;
            for (int i = 0; i < aVec.Length; i++)
            {
                dot += aVec[i] * bVec[i];
                aMag += aVec[i] * aVec[i];
                bMag += bVec[i] * bVec[i];
            }

            return (aMag != 0 && bMag != 0) ? dot / (Math.Sqrt(aMag) * Math.Sqrt(bMag)) : 0;
        }

        // Convert power string to number (e.g., "150cc" -> 150)
        private static double ParsePower(string power)
        {
            var digits = new string(power?.Where(char.IsDigit).ToArray());
            return double.TryParse(digits, out var result) ? result : 0;
        }

        // Hash string consistently to double (for categorical features)
        private static double HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            return Math.Abs(input.GetHashCode() % 1000); // Keep values in manageable range
        }

        // Recommend bikes based on user's last approved rental
        public static List<Bike> RecommendForUser(List<RentalLog> userLogs, List<Bike> availableBikes, int top = 3)
        {
            var lastRented = userLogs
                .Where(l => l.Action == "Approved")
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            if (lastRented == null) return new();

            var referenceBike = new Bike
            {
                Power = lastRented.Power,
                Category = lastRented.Category,
                EngineType = lastRented.EngineType,
                ColorOptions = lastRented.ColorOptions,
                Model = lastRented.BikeModel
            };

            return availableBikes
                .Select(b => new
                {
                    Bike = b,
                    Score = CosineSimilarity(referenceBike, b)
                })
                .OrderByDescending(x => x.Score)
                .Take(top)
                .Select(x => x.Bike)
                .ToList();
        }
    }
}
