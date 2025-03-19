namespace SolarmanV5Connector;

public class InverterSettingsService(SolarmanV5Service solarmanV5Service)
{
    private static readonly int[] addressesToRead = [108, 109, 145];

    public InverterSettingsDto? GetSettings()
    {
        var rawValues = solarmanV5Service.ReadValues(addressesToRead);
        if (rawValues == null || rawValues.Count != addressesToRead.Length)
        {
            return null;
        }

        var rawValueDict = rawValues.ToDictionary(x => x.Address, x => x.Value);

        return new InverterSettingsDto
        {
            IsSolarSellEnabled = rawValueDict[145] == 1,
            MaxChargeCurrent = rawValueDict[108],
            MaxDischargeCurrent = rawValueDict[109],
        };
    }

    public InverterSettingsDto? UpdateSettings(InverterSettingsUpdateDto settings)
    {
        var oldSettings = GetSettings();
        if (oldSettings == null)
        {
            return null;
        }

        if (settings.IsSolarSellEnabled.HasValue && oldSettings.IsSolarSellEnabled != settings.IsSolarSellEnabled)
        {
            solarmanV5Service.WriteValue(145, settings.IsSolarSellEnabled.Value ? 1 : 0);
        }

        if (settings.MaxChargeCurrent.HasValue && oldSettings.MaxChargeCurrent != settings.MaxChargeCurrent)
        {
            solarmanV5Service.WriteValue(108, settings.MaxChargeCurrent.Value);
        }

        if (settings.MaxDischargeCurrent.HasValue && oldSettings.MaxDischargeCurrent != settings.MaxDischargeCurrent)
        {
            solarmanV5Service.WriteValue(109, settings.MaxDischargeCurrent.Value);
        }

        return GetSettings();
    }
}
