namespace Web.Client.DTOs;

public class HeatPumpHourlyScheduleDto : HourlyScheduleDtoBase
{
    public int? HeatingOffset { get; set; }
    public int? HotWaterMode { get; set; }
}
