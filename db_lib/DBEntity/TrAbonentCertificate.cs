using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TrAbonentCertificate
{
    public int KeyId { get; set; }

    public int AbonentKeyId { get; set; }

    public string Thumbprint { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }

    public bool? IsActive { get; set; }

}
