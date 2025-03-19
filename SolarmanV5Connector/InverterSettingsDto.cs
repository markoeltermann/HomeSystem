namespace SolarmanV5Connector;

public class InverterSettingsDto
{
    public bool IsSolarSellEnabled { get; set; }
    public int MaxChargeCurrent { get; set; }
    public int MaxDischargeCurrent { get; set; }
}
