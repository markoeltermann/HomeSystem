using Domain;

namespace ValueReaderService.Services;
public record PointValue(DevicePoint Point, string Value, DateTime? TimeStamp)
{
    public PointValue(DevicePoint Point, string Value) : this(Point, Value, null) { }
}
