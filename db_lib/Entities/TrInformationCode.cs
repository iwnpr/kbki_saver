using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TrInformationCode
{
    public int KeyId { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<TeDlrequest> TeDlrequests { get; set; } = new List<TeDlrequest>();
}
