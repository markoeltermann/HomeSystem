namespace Web.Client.DTOs;

public class DayScheduleDtoBase<T> where T : HourlyScheduleDtoBase
{
    public T[] Hours { get; set; } = [];
}
