using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TeDlput
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public DateTime RequestDateTime { get; set; }

    public DateTime? ValidationDateTime { get; set; }

    public DateTime? ResponseDateTime { get; set; }

    public string? Guid { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentId { get; set; }

    public string? RequestId { get; set; }

    public string? RequestXml { get; set; }

    public byte[]? RequestSignedData { get; set; }

    public string? ErrorMessage { get; set; }

    public int ErrorCode { get; set; }

    public int AddCommandsCount { get; set; }

    public int DeleteCommandsCount { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    public virtual TrAbonent? Abonent { get; set; }
}
