namespace SolarmanV5Connector;

internal static class ScheduleItemDtoExtensions
{
    public static TimePointFlags GetTimePointFlags(this ScheduleItemDto dto)
    {
        if (dto.IsGridChargeEnabled)
        {
            if (dto.IsGridSellEnabled)
                return TimePointFlags.GridCharge | TimePointFlags.Sell;
            else
                return TimePointFlags.GridCharge;
        }
        else
        {
            if (dto.IsGridSellEnabled)
                return TimePointFlags.Sell;
            else
                return 0;
        }
    }
}
