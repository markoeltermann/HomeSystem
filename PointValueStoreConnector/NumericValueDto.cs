namespace PointValueStoreConnector;

public class NumericValueDto
{
    public DateTime Timestamp { get; set; }
    public double? Value { get; set; }
    public string? StringValue { get; set; }
}