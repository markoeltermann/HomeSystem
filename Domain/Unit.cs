using System;
using System.Collections.Generic;

namespace Domain;

public partial class Unit
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DevicePoint> DevicePoints { get; set; } = new List<DevicePoint>();
}
