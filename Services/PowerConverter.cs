namespace RideRental.Services
{
    public static class PowerConverter
    {
        public static double CCtoHP(double cc)
        {
            return Math.Round(cc / 32.5, 2); // approx: 32.5cc = 1 HP
        }
    }
}
