using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Таблица используется для запросов cert_add и cert_revoke
/// </summary>
public partial class TeCertManage
{
    public long KeyId { get; set; }

    public DateTime Inserted { get; set; }

    public DateTime? RequestDateTime { get; set; }

    public DateTime? ValidationDateTime { get; set; }

    public DateTime? ResponseDateTime { get; set; }

    public int ServiceType { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentId { get; set; }

    public int? CertificateId { get; set; }

    public string? RequestId { get; set; }

    public byte[]? CertData { get; set; }

    public byte[]? SignData { get; set; }

    public string? ErrorMessage { get; set; }

    public int ErrorCode { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    public string? TempGuid { get; set; }

    public virtual TrAbonent? Abonent { get; set; }

    public virtual TrAbonentCertificate? Certificate { get; set; }
}
