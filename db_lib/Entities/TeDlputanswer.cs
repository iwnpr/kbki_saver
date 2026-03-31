using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Таблица со всеми запросами dlputanswer которые пришли к нам
/// </summary>
public partial class TeDlputanswer
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public DateTime RequestDateTime { get; set; }

    public DateTime? ValidationDateTime { get; set; }

    public DateTime ResponseDateTime { get; set; }

    public string? Guid { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentId { get; set; }

    public string? ErrorMessage { get; set; }

    public int ErrorCode { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    public string TempGuid { get; set; } = null!;

    public virtual TrAbonent? Abonent { get; set; }
}
