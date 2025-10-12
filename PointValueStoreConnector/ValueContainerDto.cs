using System.ComponentModel.DataAnnotations;

namespace PointValueStoreConnector;

public class ValueContainerDto
{
    [Required]
    public NumericValueDto[] Values { get; set; } = null!;
}
