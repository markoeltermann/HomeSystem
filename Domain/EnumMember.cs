using System;
using System.Collections.Generic;

namespace Domain;

public partial class EnumMember
{
    public int DevicePointId { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = null!;

    public virtual DevicePoint DevicePoint { get; set; } = null!;
}
