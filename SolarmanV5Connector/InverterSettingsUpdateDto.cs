namespace SolarmanV5Connector;

public class InverterSettingsUpdateDto
{
    public bool? IsSolarSellEnabled { get; set; }
    public int? MaxChargeCurrent { get; set; }
    public int? MaxDischargeCurrent { get; set; }
}
