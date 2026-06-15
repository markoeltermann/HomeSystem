using System.ComponentModel;

namespace Domain;

public class ConfigPointModel
{
    [DisplayName("Is floor cooling enabled")]
    public bool IsFloorCoolingEnabled { get; set; }
}
