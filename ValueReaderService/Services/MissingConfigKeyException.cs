namespace ValueReaderService.Services;
public class MissingConfigKeyException(string configKey) : Exception($"Config key {configKey} is missing from appSettings.config")
{
    public string ConfigKey { get; } = configKey;
}
