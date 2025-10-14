using FluentAssertions;
using SolarmanV5Client.Models;
using ValueReaderService.Services.InverterSchedule;

namespace ValueReaderServiceTests.Services.InverterSchedule;
public class InverterScheduleHelpersTests
{
    [Fact]
    public void SinglePoint_ConstantSchedule()
    {
        List<ScheduleItemDto> changePoints = [new ScheduleItemDto { BatteryChargeLevel = 50 }];

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, 0);
        schedule.Should().NotBeNull();

        var items = ToArray(schedule);
        items.Should().AllSatisfy(x => x.BatteryChargeLevel.Should().Be(50));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MoreThanSixPoint_Case1(int hour)
    {
        List<ScheduleItemDto> changePoints = GetMoreThanSixPoints();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(2);
        schedule.SchedulePoint6.Time.Hour.Should().Be(16);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void MoreThanSixPoint_Case2(int hour)
    {
        List<ScheduleItemDto> changePoints = GetMoreThanSixPoints();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(8);
        schedule.SchedulePoint6.Time.Hour.Should().Be(22);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    public void MoreThanSixPoint_Case3(int hour)
    {
        List<ScheduleItemDto> changePoints = GetMoreThanSixPoints();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(12);
        schedule.SchedulePoint5.Time.Hour.Should().Be(22);
        schedule.SchedulePoint6.Time.Hour.Should().Be(23);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(15)]
    public void MoreThanSixPoint_Case4(int hour)
    {
        List<ScheduleItemDto> changePoints = GetMoreThanSixPoints();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(1);
        schedule.SchedulePoint2.BatteryChargeLevel.Should().Be(schedule.SchedulePoint1.BatteryChargeLevel);
        schedule.SchedulePoint2.IsGridChargeEnabled.Should().Be(schedule.SchedulePoint1.IsGridChargeEnabled);
        schedule.SchedulePoint3.Time.Hour.Should().Be(14);
        schedule.SchedulePoint5.Time.Hour.Should().Be(22);
        schedule.SchedulePoint6.Time.Hour.Should().Be(23);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(15)]
    public void TwoPoint_Case1(int hour)
    {
        List<ScheduleItemDto> changePoints = GetTwoPoints();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(12);
        schedule.SchedulePoint3.Time.Hour.Should().Be(13);
        schedule.SchedulePoint3.BatteryChargeLevel.Should().Be(schedule.SchedulePoint2.BatteryChargeLevel);
        schedule.SchedulePoint3.IsGridChargeEnabled.Should().Be(schedule.SchedulePoint2.IsGridChargeEnabled);
        schedule.SchedulePoint6.Time.Hour.Should().Be(16);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(23)]
    public void TwoPoint_Case2(int hour)
    {
        List<ScheduleItemDto> changePoints = GetTwoPoints();
        changePoints[^1].Time = new TimeOnly(23, 0);

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(1);
        schedule.SchedulePoint3.Time.Hour.Should().Be(2);
        schedule.SchedulePoint3.BatteryChargeLevel.Should().Be(schedule.SchedulePoint2.BatteryChargeLevel);
        schedule.SchedulePoint3.IsGridChargeEnabled.Should().Be(schedule.SchedulePoint2.IsGridChargeEnabled);
        schedule.SchedulePoint6.Time.Hour.Should().Be(23);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(15)]
    public void TwoPoint_15MinuteCase1(int hour)
    {
        List<ScheduleItemDto> changePoints = GetTwoPoints15Min();

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(12);
        schedule.SchedulePoint2.Time.Minute.Should().Be(15);
        schedule.SchedulePoint3.Time.Hour.Should().Be(13);
        schedule.SchedulePoint3.BatteryChargeLevel.Should().Be(schedule.SchedulePoint2.BatteryChargeLevel);
        schedule.SchedulePoint3.IsGridChargeEnabled.Should().Be(schedule.SchedulePoint2.IsGridChargeEnabled);
        schedule.SchedulePoint6.Time.Hour.Should().Be(16);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(15)]
    public void TwoPoint_15MinuteCase2(int hour)
    {
        List<ScheduleItemDto> changePoints = GetTwoPoints15Min();
        changePoints[^1].Time = new TimeOnly(23, 45);

        var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, hour);
        schedule.Should().NotBeNull();
        schedule.SchedulePoint1.Time.Hour.Should().Be(0);
        schedule.SchedulePoint2.Time.Hour.Should().Be(1);
        schedule.SchedulePoint3.Time.Hour.Should().Be(2);
        schedule.SchedulePoint4.Time.Hour.Should().Be(3);
        schedule.SchedulePoint5.Time.Hour.Should().Be(4);
        schedule.SchedulePoint6.Time.Hour.Should().Be(23);
        schedule.SchedulePoint6.Time.Minute.Should().Be(45);
    }

    private static List<ScheduleItemDto> GetMoreThanSixPoints()
    {
        return [
            new ScheduleItemDto
            {
                Time = new TimeOnly(0, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = true,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(2, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = false,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(8, 0),
                BatteryChargeLevel = 65,
                IsGridChargeEnabled = false,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(12, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = false,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(14, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = true,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(16, 0),
                BatteryChargeLevel = 25,
                IsGridChargeEnabled = false,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(22, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = false,
            },
        ];
    }

    private static List<ScheduleItemDto> GetTwoPoints()
    {
        return [
            new ScheduleItemDto
            {
                Time = new TimeOnly(0, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = true,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(12, 0),
                BatteryChargeLevel = 50,
                IsGridChargeEnabled = false,
            },
        ];
    }

    private static List<ScheduleItemDto> GetTwoPoints15Min()
    {
        return [
            new ScheduleItemDto
            {
                Time = new TimeOnly(0, 0),
                BatteryChargeLevel = 100,
                IsGridChargeEnabled = true,
            },
            new ScheduleItemDto
            {
                Time = new TimeOnly(12, 15),
                BatteryChargeLevel = 50,
                IsGridChargeEnabled = false,
            },
        ];
    }

    private static ScheduleItemDto[] ToArray(ScheduleDto schedule)
    {
        return [schedule.SchedulePoint1, schedule.SchedulePoint2, schedule.SchedulePoint3, schedule.SchedulePoint4, schedule.SchedulePoint5, schedule.SchedulePoint6];
    }
}
