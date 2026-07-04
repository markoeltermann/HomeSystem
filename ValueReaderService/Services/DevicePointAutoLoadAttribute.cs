namespace ValueReaderService.Services;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class DevicePointAutoLoadAttribute(string deviceType, string? deviceSubType, string devicePointType) : Attribute
{
    public string DeviceType { get; } = deviceType;

    public string? DeviceSubType { get; } = deviceSubType;

    public string DevicePointType { get; } = devicePointType;
}
