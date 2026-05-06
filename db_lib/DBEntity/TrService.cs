using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TrService
{
    public int KeyId { get; set; }

    public string? ServiceName { get; set; }

    public string? Description { get; set; }
}
