using System.ComponentModel;

namespace Domain;

public class ConfigPointModel
{
    [DisplayName("Is floor cooling enabled")]
    public bool IsFloorCoolingEnabled { get; set; }

    [DisplayName("VAT")]
    public decimal VAT { get; set; }
}
