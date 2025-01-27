namespace SolarmanV5Connector;

public class ScheduleService(SolarmanV5Service solarmanV5Service)
{
    private static readonly int[] addressesToRead = [148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177];
    public ScheduleDto? GetSchedule()
    {
        var rawValues = solarmanV5Service.ReadValues(addressesToRead);

        if (rawValues == null || rawValues.Count != addressesToRead.Length)
        {
            return null;
        }

        var rawValueDict = rawValues.ToDictionary(x => x.Address, x => x.Value);

        var schedule = new ScheduleDto();

        schedule.SchedulePoint1.Time = GetTime(rawValueDict[148]);
        schedule.SchedulePoint2.Time = GetTime(rawValueDict[149]);
        schedule.SchedulePoint3.Time = GetTime(rawValueDict[150]);
        schedule.SchedulePoint4.Time = GetTime(rawValueDict[151]);
        schedule.SchedulePoint5.Time = GetTime(rawValueDict[152]);
        schedule.SchedulePoint6.Time = GetTime(rawValueDict[153]);

        schedule.SchedulePoint1.MaxBatteryPower = rawValueDict[154] * 10;
        schedule.SchedulePoint2.MaxBatteryPower = rawValueDict[155] * 10;
        schedule.SchedulePoint3.MaxBatteryPower = rawValueDict[156] * 10;
        schedule.SchedulePoint4.MaxBatteryPower = rawValueDict[157] * 10;
        schedule.SchedulePoint5.MaxBatteryPower = rawValueDict[158] * 10;
        schedule.SchedulePoint6.MaxBatteryPower = rawValueDict[159] * 10;

        schedule.SchedulePoint1.BatteryChargeLevel = rawValueDict[166];
        schedule.SchedulePoint2.BatteryChargeLevel = rawValueDict[167];
        schedule.SchedulePoint3.BatteryChargeLevel = rawValueDict[168];
        schedule.SchedulePoint4.BatteryChargeLevel = rawValueDict[169];
        schedule.SchedulePoint5.BatteryChargeLevel = rawValueDict[170];
        schedule.SchedulePoint6.BatteryChargeLevel = rawValueDict[171];

        schedule.SchedulePoint1.IsGridChargeEnabled = (rawValueDict[172] & 0x01) > 0;
        schedule.SchedulePoint2.IsGridChargeEnabled = (rawValueDict[173] & 0x01) > 0;
        schedule.SchedulePoint3.IsGridChargeEnabled = (rawValueDict[174] & 0x01) > 0;
        schedule.SchedulePoint4.IsGridChargeEnabled = (rawValueDict[175] & 0x01) > 0;
        schedule.SchedulePoint5.IsGridChargeEnabled = (rawValueDict[176] & 0x01) > 0;
        schedule.SchedulePoint6.IsGridChargeEnabled = (rawValueDict[177] & 0x01) > 0;

        return schedule;
    }

    public ScheduleDto? UpdateSchedule(ScheduleDto schedule)
    {
        Validate(schedule);

        var oldSchedule = GetSchedule();
        if (oldSchedule == null)
        {
            return null;
        }

        if (oldSchedule.SchedulePoint1.Time != schedule.SchedulePoint1.Time)
        {
            solarmanV5Service.WriteValue(148, GetTime(schedule.SchedulePoint1.Time));
        }
        if (oldSchedule.SchedulePoint2.Time != schedule.SchedulePoint2.Time)
        {
            solarmanV5Service.WriteValue(149, GetTime(schedule.SchedulePoint2.Time));
        }
        if (oldSchedule.SchedulePoint3.Time != schedule.SchedulePoint3.Time)
        {
            solarmanV5Service.WriteValue(150, GetTime(schedule.SchedulePoint3.Time));
        }
        if (oldSchedule.SchedulePoint4.Time != schedule.SchedulePoint4.Time)
        {
            solarmanV5Service.WriteValue(151, GetTime(schedule.SchedulePoint4.Time));
        }
        if (oldSchedule.SchedulePoint5.Time != schedule.SchedulePoint5.Time)
        {
            solarmanV5Service.WriteValue(152, GetTime(schedule.SchedulePoint5.Time));
        }
        if (oldSchedule.SchedulePoint6.Time != schedule.SchedulePoint6.Time)
        {
            solarmanV5Service.WriteValue(153, GetTime(schedule.SchedulePoint6.Time));
        }

        if (oldSchedule.SchedulePoint1.MaxBatteryPower != schedule.SchedulePoint1.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(154, schedule.SchedulePoint1.MaxBatteryPower / 10);
        }
        if (oldSchedule.SchedulePoint2.MaxBatteryPower != schedule.SchedulePoint2.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(155, schedule.SchedulePoint2.MaxBatteryPower / 10);
        }
        if (oldSchedule.SchedulePoint3.MaxBatteryPower != schedule.SchedulePoint3.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(156, schedule.SchedulePoint3.MaxBatteryPower / 10);
        }
        if (oldSchedule.SchedulePoint4.MaxBatteryPower != schedule.SchedulePoint4.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(157, schedule.SchedulePoint4.MaxBatteryPower / 10);
        }
        if (oldSchedule.SchedulePoint5.MaxBatteryPower != schedule.SchedulePoint5.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(158, schedule.SchedulePoint5.MaxBatteryPower / 10);
        }
        if (oldSchedule.SchedulePoint6.MaxBatteryPower != schedule.SchedulePoint6.MaxBatteryPower)
        {
            solarmanV5Service.WriteValue(159, schedule.SchedulePoint6.MaxBatteryPower / 10);
        }

        if (oldSchedule.SchedulePoint1.BatteryChargeLevel != schedule.SchedulePoint1.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(166, schedule.SchedulePoint1.BatteryChargeLevel);
        }
        if (oldSchedule.SchedulePoint2.BatteryChargeLevel != schedule.SchedulePoint2.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(167, schedule.SchedulePoint2.BatteryChargeLevel);
        }
        if (oldSchedule.SchedulePoint3.BatteryChargeLevel != schedule.SchedulePoint3.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(168, schedule.SchedulePoint3.BatteryChargeLevel);
        }
        if (oldSchedule.SchedulePoint4.BatteryChargeLevel != schedule.SchedulePoint4.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(169, schedule.SchedulePoint4.BatteryChargeLevel);
        }
        if (oldSchedule.SchedulePoint5.BatteryChargeLevel != schedule.SchedulePoint5.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(170, schedule.SchedulePoint5.BatteryChargeLevel);
        }
        if (oldSchedule.SchedulePoint6.BatteryChargeLevel != schedule.SchedulePoint6.BatteryChargeLevel)
        {
            solarmanV5Service.WriteValue(171, schedule.SchedulePoint6.BatteryChargeLevel);
        }

        if (oldSchedule.SchedulePoint1.IsGridChargeEnabled != schedule.SchedulePoint1.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(172, schedule.SchedulePoint1.IsGridChargeEnabled ? 1 : 0);
        }
        if (oldSchedule.SchedulePoint2.IsGridChargeEnabled != schedule.SchedulePoint2.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(173, schedule.SchedulePoint2.IsGridChargeEnabled ? 1 : 0);
        }
        if (oldSchedule.SchedulePoint3.IsGridChargeEnabled != schedule.SchedulePoint3.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(174, schedule.SchedulePoint3.IsGridChargeEnabled ? 1 : 0);
        }
        if (oldSchedule.SchedulePoint4.IsGridChargeEnabled != schedule.SchedulePoint4.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(175, schedule.SchedulePoint4.IsGridChargeEnabled ? 1 : 0);
        }
        if (oldSchedule.SchedulePoint5.IsGridChargeEnabled != schedule.SchedulePoint5.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(176, schedule.SchedulePoint5.IsGridChargeEnabled ? 1 : 0);
        }
        if (oldSchedule.SchedulePoint6.IsGridChargeEnabled != schedule.SchedulePoint6.IsGridChargeEnabled)
        {
            solarmanV5Service.WriteValue(177, schedule.SchedulePoint6.IsGridChargeEnabled ? 1 : 0);
        }

        return GetSchedule();
    }

    private static void Validate(ScheduleDto schedule)
    {
        Validate(schedule.SchedulePoint1);
        Validate(schedule.SchedulePoint2);
        Validate(schedule.SchedulePoint3);
        Validate(schedule.SchedulePoint4);
        Validate(schedule.SchedulePoint5);
        Validate(schedule.SchedulePoint6);
    }

    private static void Validate(ScheduleItemDto schedulePoint)
    {
        if (schedulePoint == null)
        {
            throw new BadRequestException("A schedule point is missing.");
        }

        if (schedulePoint.MaxBatteryPower is < 0 or > 10000)
        {
            throw new BadRequestException("Max battery power is out of range.");
        }

        if (schedulePoint.BatteryChargeLevel is < 0 or > 100)
        {
            throw new BadRequestException("Battery charge level is out of range.");
        }
    }

    private static TimeOnly GetTime(int value)
    {
        var hours = value / 100;
        var minutes = value % 100;

        if (hours is < 0 or > 23 || minutes is < 0 or > 59)
        {
            throw new BadRequestException("Invalid time value received.");
        }

        return new TimeOnly(hours, minutes);
    }

    private static int GetTime(TimeOnly time)
    {
        return time.Minute + time.Hour * 100;
    }
}
