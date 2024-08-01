namespace Web.Client.DTOs;

public class ValueContainerDto
{
    public NumericValueDto[] Values { get; set; } = null!;
    public string Unit { get; set; } = null!;
}
