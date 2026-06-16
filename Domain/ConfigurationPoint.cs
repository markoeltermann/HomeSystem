using System;
using System.Collections.Generic;

namespace Domain;

public partial class ConfigurationPoint
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public string Value { get; set; } = null!;
}
