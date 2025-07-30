using System;
using System.Collections.Generic;

namespace Domain;

public partial class Device
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsEnabled { get; set; }

    public string Type { get; set; } = null!;

    public string? SubType { get; set; }

    public virtual ICollection<DevicePoint> DevicePoints { get; set; } = new List<DevicePoint>();
}
