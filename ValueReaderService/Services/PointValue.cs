using Domain;

namespace ValueReaderService.Services;
public record PointValue(DevicePoint Point, string Value, DateTime? Timestamp)
{
    public PointValue(DevicePoint Point, string Value) : this(Point, Value, null) { }
}
