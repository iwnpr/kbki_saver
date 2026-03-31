using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Тип ответа. Результативный/не результативный
/// </summary>
public partial class TrAmpResponseType
{
    public int KeyId { get; set; }

    public string ResponseData { get; set; } = null!;

    public string WhoseData { get; set; } = null!;

    public virtual ICollection<TeResponse> TeResponses { get; set; } = new List<TeResponse>();
}
