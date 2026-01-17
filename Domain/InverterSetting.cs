using System;
using System.Collections.Generic;

namespace Domain;

public partial class InverterSetting
{
    public short BatteryChargeCurrent { get; set; }

    public short BatteryDischargeCurrent { get; set; }

    public short BatteryDischargeCurrentBelow30 { get; set; }

    public short BatteryDischargeCurrentBelow20 { get; set; }
}
