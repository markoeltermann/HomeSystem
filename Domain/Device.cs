using System;
using System.Collections.Generic;

namespace Domain;

public partial class Device
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsEnabled { get; set; }

    public virtual ICollection<DevicePoint> DevicePoints { get; } = new List<DevicePoint>();
}
