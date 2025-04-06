namespace RideRental.Services
{
    using RideRental.Models;

    public static class BikeRecommender
    { 
        public static double CosineSimilarity(Bike a, Bike b)
        {
            var aVec = new[] {
            ParsePower(a.Power),
            a.Year
        };

            var bVec = new[] {
            ParsePower(b.Power),
            b.Year
        };

            double dot = 0, aMag = 0, bMag = 0;
            for (int i = 0; i < aVec.Length; i++)
            {
                dot += aVec[i] * bVec[i];
                aMag += aVec[i] * aVec[i];
                bMag += bVec[i] * bVec[i];
            }

            return dot / (Math.Sqrt(aMag) * Math.Sqrt(bMag));
        }
 
        private static double ParsePower(string power)
        {
            var digits = new string(power?.Where(char.IsDigit).ToArray());
            return double.TryParse(digits, out var result) ? result : 0;
        }

        // MAIN: Recommend bikes for user based on past rentals
        public static List<Bike> RecommendForUser(List<RentalLog> userLogs, List<Bike> availableBikes, int top = 3)
        {
            // Group logs by bike and select the most recent bike
            var lastRented = userLogs
                .Where(l => l.Action == "Approved")
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            if (lastRented == null) return new();

            // Reconstruct a pseudo-bike from RentalLog (acts as reference)
            var referenceBike = new Bike
            {
                Power = lastRented.Power,
                Year = 0, // you can omit or average it if not available
            };

            return availableBikes
                .Select(b => new {
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
