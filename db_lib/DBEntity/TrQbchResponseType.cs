using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TrQbchResponseType
{
    public int KeyId { get; set; }

    public string ResponseData { get; set; } = null!;

    public string WhoseData { get; set; } = null!;

}
