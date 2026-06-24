using System.ComponentModel;

namespace Domain;

public class ConfigPointModel
{
    [DisplayName("Is floor cooling set up")]
    public bool IsFloorCoolingEnabled { get; set; }

    [DisplayName("Is floor cooling allowed")]
    public bool IsFloorCoolingAllowed { get; set; }

    [DisplayName("Floor cooling setpoint")]
    public decimal FloorCoolingSetpoint { get; set; }

    [DisplayName("VAT")]
    public decimal VAT { get; set; }
}
