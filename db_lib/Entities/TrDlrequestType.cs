using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Тип запроса
/// 1 = Только наши данные
/// 2 = Одно окно (Нужно собирать со всех КБКИ)
/// </summary>
public partial class TrDlrequestType
{
    public int KeyId { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<TeDlrequest> TeDlrequests { get; set; } = new List<TeDlrequest>();
}
