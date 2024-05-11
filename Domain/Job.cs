using System;
using System.Collections.Generic;

namespace Domain;

public partial class Job
{
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string Name { get; set; } = null!;
}
