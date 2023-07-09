namespace PhiZoneApi.Utils;

public static class ChartUtil
{
    public static double ConvertTime(List<int> time)
    {
        if (time[1] == 0 || time[2] == 0)
        {
            return time[0];
        }

        return time[0] + (double)time[1] / time[2];
    }
}