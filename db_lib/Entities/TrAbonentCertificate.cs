using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Список сертификатов связанных с абонентом
/// </summary>
public partial class TrAbonentCertificate
{
    public int KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public int AbonentId { get; set; }

    public string Thumbprint { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual TrAbonent Abonent { get; set; } = null!;

    public virtual ICollection<TeCertManage> TeCertManages { get; set; } = new List<TeCertManage>();
}
