using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TrUserType
{
    public int KeyId { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<TdUser> TdUsers { get; set; } = new List<TdUser>();

    public virtual ICollection<TrAbonent> TrAbonents { get; set; } = new List<TrAbonent>();
}
