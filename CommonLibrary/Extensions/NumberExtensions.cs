namespace CommonLibrary.Extensions;

public static class NumberExtensions
{
    public static int Truncate(this int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }
}
