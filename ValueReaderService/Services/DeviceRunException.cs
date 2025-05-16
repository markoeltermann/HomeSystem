namespace ValueReaderService.Services;
public class DeviceRunException : Exception
{
    public DeviceRunException() { }

    public DeviceRunException(string message) : base(message) { }
}
