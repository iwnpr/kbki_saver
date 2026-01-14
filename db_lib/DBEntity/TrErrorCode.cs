using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TrErrorCode
{
    public int KeyId { get; set; }

    public string Description { get; set; } = null!;

    public string? Comments { get; set; }

}
