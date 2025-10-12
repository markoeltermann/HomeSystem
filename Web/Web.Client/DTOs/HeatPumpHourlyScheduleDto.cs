namespace Web.Client.DTOs;

public class HeatPumpHourlyScheduleDto : ScheduleDtoBase
{
    public int? HeatingOffset { get; set; }
    public int? HotWaterMode { get; set; }
}
