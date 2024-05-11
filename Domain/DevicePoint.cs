using System;
using System.Collections.Generic;

namespace Domain;

public partial class DevicePoint
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int DeviceId { get; set; }

    public int DataTypeId { get; set; }

    public virtual DataType DataType { get; set; } = null!;

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<EnumMember> EnumMembers { get; } = new List<EnumMember>();
}
