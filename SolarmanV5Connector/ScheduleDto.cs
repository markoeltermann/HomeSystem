namespace SolarmanV5Connector;

public class ScheduleDto
{
    public ScheduleDto()
    {
        SchedulePoint1 = new();
        SchedulePoint2 = new();
        SchedulePoint3 = new();
        SchedulePoint4 = new();
        SchedulePoint5 = new();
        SchedulePoint6 = new();
    }

    public ScheduleItemDto SchedulePoint1 { get; set; }
    public ScheduleItemDto SchedulePoint2 { get; set; }
    public ScheduleItemDto SchedulePoint3 { get; set; }
    public ScheduleItemDto SchedulePoint4 { get; set; }
    public ScheduleItemDto SchedulePoint5 { get; set; }
    public ScheduleItemDto SchedulePoint6 { get; set; }
}
