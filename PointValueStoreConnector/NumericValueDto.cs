using System.ComponentModel.DataAnnotations;

namespace PointValueStoreConnector;

public class NumericValueDto
{
    [Required]
    public DateTime Timestamp { get; set; }
    public double? Value { get; set; }
    public string? StringValue { get; set; }
}