using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TdPermission
{
    public int KeyId { get; set; }

    public int AbonentsKeyId { get; set; }

    public int ServicesKeyId { get; set; }

    public bool IsGranted { get; set; }

  
}
