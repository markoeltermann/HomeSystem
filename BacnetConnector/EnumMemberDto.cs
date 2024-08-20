namespace BacnetConnector;

public record EnumMemberDto
{
    public int Value { get; set; }
    public string Name { get; set; } = null!;
}
