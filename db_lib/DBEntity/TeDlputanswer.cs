using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeDlputanswer
{
    public long KeyId { get; set; }

    public string? DlputanswerId { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentKeyId { get; set; }

    public DateTime RequestDateTime { get; set; }

    public DateTime? ValidationDateTime { get; set; }

    public DateTime ResponseDateTime { get; set; }

    public string? ErrorMessage { get; set; }

    public int? ErrorCodeKeyId { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    public string TempGuid { get; set; } = null!;
}
