namespace Leatherworks
{
    /// <summary>Pure stud-bonus math, kept free of Verse types so tests can call it.</summary>
    public static class StudMath
    {
        public const float StudFactor = 0.3f;

        public static float Bonus(float metalStuffPower, float studFactor)
        {
            return metalStuffPower * studFactor;
        }
    }

    public static class StudLabel
    {
        public static string Format(string label, string metalLabel)
        {
            return string.IsNullOrEmpty(metalLabel) ? label : $"{label} ({metalLabel})";
        }
    }
}
