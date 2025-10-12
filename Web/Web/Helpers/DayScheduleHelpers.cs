using Web.Client.DTOs;
using WebCommonLibrary;

namespace Web.Helpers;

public static class DayScheduleHelpers
{
    public static void ValidateDaySchedule<T>(DayScheduleDtoBase<T> daySchedule, bool is15MinuteSchedule) where T : ScheduleDtoBase
    {
        var expectedHours = is15MinuteSchedule ? 96 : 24;

        if (daySchedule == null || daySchedule.Entries == null || daySchedule.Entries.Length != expectedHours)
        {
            throw new BadRequestException("Invalid schedule");
        }

        var hours = daySchedule.Entries.OrderBy(x => x.Hour).ToArray();
        for (int i = 0; i < expectedHours; i++)
        {
            if (hours[i].Hour != i)
            {
                throw new BadRequestException("Invalid schedule");
            }
        }
    }
}
