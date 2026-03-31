using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Права доступа к запросам КБКИ
/// </summary>
public partial class TdPermission
{
    public int KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public int AbonentId { get; set; }

    public int ServiceId { get; set; }

    public bool IsGranted { get; set; }

    public virtual TrAbonent Abonent { get; set; } = null!;

    public virtual TrService Service { get; set; } = null!;
}
