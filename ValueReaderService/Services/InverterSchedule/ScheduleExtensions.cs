using SolarmanV5Client.Models;

namespace ValueReaderService.Services.InverterSchedule;
internal static class ScheduleExtensions
{
    public static IEnumerable<ScheduleItemDto> GetItems(this ScheduleDto schedule)
    {
        yield return schedule.SchedulePoint1!;
        yield return schedule.SchedulePoint2!;
        yield return schedule.SchedulePoint3!;
        yield return schedule.SchedulePoint4!;
        yield return schedule.SchedulePoint5!;
        yield return schedule.SchedulePoint6!;
    }
}
