using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TrSpResponseType
{
    public int KeyId { get; set; }

    public string ResponseData { get; set; } = null!;

    public virtual ICollection<TeResponse> TeResponses { get; set; } = new List<TeResponse>();
}
