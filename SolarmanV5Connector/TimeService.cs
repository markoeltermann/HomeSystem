namespace SolarmanV5Connector;

public class TimeService(SolarmanV5Service solarmanV5Service)
{
    public DateTime? GetTime()
    {
        var values = solarmanV5Service.ReadValues([62, 63, 64]);
        if (values == null || values.Count != 3)
        {
            return null;
        }

        var reg62 = values.FirstOrDefault(x => x.Address == 62)?.Value;
        var reg63 = values.FirstOrDefault(x => x.Address == 63)?.Value;
        var reg64 = values.FirstOrDefault(x => x.Address == 64)?.Value;

        if (reg62 == null || reg63 == null || reg64 == null)
        {
            return null;
        }

        var year = (reg62.Value >> 8) + 2000;
        var month = reg62.Value & 0xFF;
        var day = reg63.Value >> 8;
        var hour = reg63.Value & 0xFF;
        var minute = reg64.Value >> 8;
        var second = reg64.Value & 0xFF;

        try
        {
            return new DateTime(year, month, day, hour, minute, second);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public DateTime? SetTime(DateTime time)
    {
        var reg62 = ((time.Year - 2000) << 8) | time.Month;
        var reg63 = (time.Day << 8) | time.Hour;
        var reg64 = (time.Minute << 8) | time.Second;

        var success = solarmanV5Service.WriteValue(62, reg62)
            && solarmanV5Service.WriteValue(63, reg63)
            && solarmanV5Service.WriteValue(64, reg64);

        if (success)
        {
            return GetTime();
        }
        else
        {
            return null;
        }
    }
}
