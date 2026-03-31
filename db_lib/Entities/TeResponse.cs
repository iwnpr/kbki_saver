using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Ответы собранные в qbch_tasks будут разложены в данную таблицу вне зависимости от типа - пакетный запрос или одиночный.
/// </summary>
public partial class TeResponse
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public long QbchTaskId { get; set; }

    public int? OrderNum { get; set; }

    public int? AmpResponseType { get; set; }

    public int? SpResponseType { get; set; }

    public int ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ResponseXml { get; set; }

    public virtual TrAmpResponseType? AmpResponseTypeNavigation { get; set; }

    public virtual TeQbchTask QbchTask { get; set; } = null!;

    public virtual TrSpResponseType? SpResponseTypeNavigation { get; set; }
}
