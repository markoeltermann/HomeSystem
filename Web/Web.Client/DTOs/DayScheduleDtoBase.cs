namespace Web.Client.DTOs;

public class DayScheduleDtoBase<T> where T : ScheduleDtoBase
{
    public T[] Entries { get; set; } = [];
}
