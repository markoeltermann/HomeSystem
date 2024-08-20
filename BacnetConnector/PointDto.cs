namespace BacnetConnector;

public record PointDto
{
    public string? Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PresentValue { get; set; } = null!;
    public EnumMemberDto[]? PossibleValues { get; set; }
}
