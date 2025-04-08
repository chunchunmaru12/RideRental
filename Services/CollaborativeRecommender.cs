using RideRental.Models;

namespace RideRental.Services
{
    public static class CollaborativeRecommender
    {
        private static readonly Dictionary<string, int> modelIndex = new();
        private static int nextModelIndex = 1;

        public static List<Bike> RecommendForUser(string userEmail, List<RentalLog> allLogs, List<Bike> availableBikes, int top = 3)
        {
            var userLogs = allLogs
                .Where(log => log.UserEmail == userEmail && (log.Action == "Approved" || log.Action == "Returned"))
                .ToList();

            if (!userLogs.Any()) return new();

            var userVector = userLogs
                .Select(BikeToVector)
                .Aggregate((a, b) => a.Zip(b, (x, y) => x + y).ToArray());

            double magnitude = Math.Sqrt(userVector.Sum(x => x * x));
            userVector = userVector.Select(x => x / magnitude).ToArray();

            return availableBikes
                .Select(b => new
                {
                    Bike = b,
                    Score = CosineSimilarity(userVector, BikeToVector(b))
                })
                .OrderByDescending(x => x.Score)
                .Take(top)
                .Select(x => x.Bike)
                .ToList();
        }

        public static List<Bike> RecommendForUserFromLog(RentalLog inputLog, List<Bike> availableBikes, int top = 3)
        {
            var userVector = BikeToVector(inputLog);
            double magnitude = Math.Sqrt(userVector.Sum(x => x * x));
            if (magnitude > 0)
                userVector = userVector.Select(x => x / magnitude).ToArray();

            return availableBikes
                .Select(b => new
                {
                    Bike = b,
                    Score = CosineSimilarity(userVector, BikeToVector(b))
                })
                .OrderByDescending(x => x.Score)
                .Take(top)
                .Select(x => x.Bike)
                .ToList();
        }

        private static double[] BikeToVector(Bike b)
        {
            return new double[]
            {
                ParsePower(b.Power),
                EncodeCategory(b.Category),
                EncodeEngineType(b.EngineType),
                EncodeModel(b.Model)
            };
        }

        private static double[] BikeToVector(RentalLog log)
        {
            return new double[]
            {
                ParsePower(log.Power),
                EncodeCategory(log.Category),
                EncodeEngineType(log.EngineType),
                EncodeModel(log.BikeModel)
            };
        }

        private static double ParsePower(string power)
        {
            if (string.IsNullOrEmpty(power)) return 0;

            var digits = new string(power.Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (!double.TryParse(digits, out var val)) return 0;

            // Threshold: >100 likely means it's CC
            return val > 100 ? PowerConverter.CCtoHP(val) : val;
        }

        private static double EncodeCategory(string category) =>
            category?.Trim().ToLower() switch
            {
                "sport" => 1,
                "cruiser" => 2,
                "touring" => 3,
                "allround" => 4,
                "scooter" => 5,
                "classic" => 6,
                "offroad" => 7,
                _ => 0
            };

        private static double EncodeEngineType(string engine)
        {
            if (string.IsNullOrEmpty(engine)) return 0;
            engine = engine.ToLower();
            if (engine.Contains("v2")) return 1;
            if (engine.Contains("single")) return 2;
            if (engine.Contains("twin")) return 3;
            if (engine.Contains("four-stroke")) return 4;
            if (engine.Contains("two-stroke")) return 5;
            return 0;
        }

        private static double EncodeModel(string model)
        {
            if (string.IsNullOrEmpty(model)) return 0;

            model = model.ToLower().Trim();
            if (!modelIndex.ContainsKey(model))
            {
                modelIndex[model] = nextModelIndex++;
            }
            return modelIndex[model];
        }

        private static double CosineSimilarity(double[] a, double[] b)
        {
            double dot = 0, aMag = 0, bMag = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                aMag += a[i] * a[i];
                bMag += b[i] * b[i];
            }

            return dot / (Math.Sqrt(aMag) * Math.Sqrt(bMag) + 1e-10); // prevent divide-by-zero
        }
    }
}
