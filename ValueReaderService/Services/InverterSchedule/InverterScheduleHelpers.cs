using SolarmanV5Client.Models;

namespace ValueReaderService.Services.InverterSchedule;
public static class InverterScheduleHelpers
{
    public static ScheduleDto GetCurrentSchedule(List<ScheduleItemDto> changePoints, int hour)
    {
        ScheduleDto schedule;

        if (changePoints.Count == 1)
        {
            schedule = new ScheduleDto();
            var schedulePoint = new ScheduleItemDto
            {
                Time = new TimeOnly(0, 0),
                BatteryChargeLevel = changePoints[0].BatteryChargeLevel,
                IsGridChargeEnabled = changePoints[0].IsGridChargeEnabled,
                MaxBatteryPower = 10000,
            };
            schedule.SchedulePoint1 = schedulePoint;
            schedule.SchedulePoint2 = schedulePoint;
            schedule.SchedulePoint3 = schedulePoint;
            schedule.SchedulePoint4 = schedulePoint;
            schedule.SchedulePoint5 = schedulePoint;
            schedule.SchedulePoint6 = schedulePoint;
        }
        else
        {
            var startingPointIndex = changePoints.Count - 1;
            for (var i = 0; i < changePoints.Count - 1; i++)
            {
                var c = changePoints[i];
                var n = changePoints[i + 1];
                if (n.Time.Hour > hour && c.Time.Hour <= hour)
                {
                    startingPointIndex = i;
                    break;
                }
            }

            var schedulePoints = new List<ScheduleItemDto>
            {
                new() {
                    Time = new TimeOnly(0, 0),
                    BatteryChargeLevel = changePoints[0].BatteryChargeLevel,
                    IsGridChargeEnabled = changePoints[0].IsGridChargeEnabled,
                    MaxBatteryPower = 10000,
                }
            };
            if (startingPointIndex == 0)
                startingPointIndex = 1;
            for (var i = startingPointIndex; i < Math.Min(startingPointIndex + 5, changePoints.Count); i++)
            {
                var c = changePoints[i];

                schedulePoints.Add(c);
            }

            var initialCount = schedulePoints.Count;
            if (initialCount < 6)
            {
                for (int i = 0; i < 6 - initialCount; i++)
                {
                    var last = schedulePoints[^1];
                    if (last.Time.Hour < 23)
                    {
                        schedulePoints.Add(new ScheduleItemDto
                        {
                            Time = new TimeOnly(last.Time.Hour + 1, last.Time.Minute),
                            BatteryChargeLevel = last.BatteryChargeLevel,
                            IsGridChargeEnabled = last.IsGridChargeEnabled,
                            MaxBatteryPower = last.MaxBatteryPower,
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            initialCount = schedulePoints.Count;
            if (initialCount < 6)
            {
                for (int i = 0; i < 6 - initialCount; i++)
                {
                    for (int j = 0; j < schedulePoints.Count - 1; j++)
                    {
                        var c = schedulePoints[j];
                        var n = schedulePoints[j + 1];
                        if (n.Time.Hour - c.Time.Hour > 1)
                        {
                            schedulePoints.Insert(j + 1, new ScheduleItemDto
                            {
                                Time = new TimeOnly(c.Time.Hour + 1, c.Time.Minute),
                                BatteryChargeLevel = c.BatteryChargeLevel,
                                IsGridChargeEnabled = c.IsGridChargeEnabled,
                                MaxBatteryPower = c.MaxBatteryPower,
                            });
                            break;
                        }
                    }
                }
            }

            //schedule = new ScheduleDto
            //{
            //    SchedulePoint1 = new ScheduleItemDto
            //    {
            //        Time = new TimeOnly(0, 0),
            //        BatteryChargeLevel = (int)batteryLevelChangePoints[0].Value!.Value,
            //        IsGridChargeEnabled = gridChargeEnableValues.Values[0].Value!.Value > 0.0
            //    }
            //};

            schedule = new ScheduleDto
            {
                SchedulePoint1 = schedulePoints[0],
                SchedulePoint2 = schedulePoints[1],
                SchedulePoint3 = schedulePoints[2],
                SchedulePoint4 = schedulePoints[3],
                SchedulePoint5 = schedulePoints[4],
                SchedulePoint6 = schedulePoints[5],
            };
        }
        return schedule;
    }
}
