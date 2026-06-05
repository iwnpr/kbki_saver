using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Справочник видов пользователей кредитной истории.
/// Соответствует XSD-типу СправочникВидыПользователя и атрибуту КодВидаПользователя.
/// </summary>
public partial class TrUserTypeCode
{
    public int Code { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<TdUser> TdUsers { get; set; } = new List<TdUser>();

    public virtual ICollection<TrAbonent> TrAbonents { get; set; } = new List<TrAbonent>();
}
