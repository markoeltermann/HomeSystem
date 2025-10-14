using System.ComponentModel.DataAnnotations;

namespace SolarmanV5Connector;

public class ScheduleItemDto
{
    [Required]
    public TimeOnly Time { get; set; }

    public bool IsGridChargeEnabled { get; set; }

    public bool IsGridSellEnabled { get; set; }

    public int MaxBatteryPower { get; set; }

    public int BatteryChargeLevel { get; set; }
}
