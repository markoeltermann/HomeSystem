﻿using System;
using System.Collections.Generic;

namespace Domain;

public partial class DataType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DevicePoint> DevicePoints { get; } = new List<DevicePoint>();
}
