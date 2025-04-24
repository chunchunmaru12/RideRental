namespace RideRental.Services
{
    public static class PowerConverter
    {
        public static double CCtoHP(double cc)
        {
            return Math.Round(cc / 15, 2); // approx: 15cc = 1 HP
        }
    }
}
