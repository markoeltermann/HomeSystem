using Web.Client.DTOs;
using WebCommonLibrary;

namespace Web.Helpers;

public static class DayScheduleHelpers
{
    public static void ValidateDaySchedule<T>(DayScheduleDtoBase<T> daySchedule) where T : HourlyScheduleDtoBase
    {
        if (daySchedule == null || daySchedule.Hours == null || daySchedule.Hours.Length != 24)
        {
            throw new BadRequestException("Invalid schedule");
        }

        var hours = daySchedule.Hours.OrderBy(x => x.Hour).ToArray();
        for (int i = 0; i < 24; i++)
        {
            if (hours[i].Hour != i)
            {
                throw new BadRequestException("Invalid schedule");
            }
        }
    }
}
