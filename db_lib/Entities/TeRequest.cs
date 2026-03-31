using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Запросы поступившие в dlrequest будут разложены по одному в данную таблицу, вне зависимости от типа запроса (пакетный/одиночный).
/// </summary>
public partial class TeRequest
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public long DlrequestId { get; set; }

    public long? UserId { get; set; }

    public string? RequestXml { get; set; }

    public int? OrderNum { get; set; }

    public int ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual TeDlrequest Dlrequest { get; set; } = null!;

    public virtual TrErrorCode ErrorCodeNavigation { get; set; } = null!;

    public virtual ICollection<TeSubject> TeSubjects { get; set; } = new List<TeSubject>();

    public virtual TdUser? User { get; set; }
}
