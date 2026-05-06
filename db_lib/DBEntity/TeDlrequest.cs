using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeDlrequest : IDisposable
{
    public long KeyId { get; set; }

    public string DlanswerId { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? RequestCertificateThumbprint { get; set; }

    public int? AbonentKeyId { get; set; }

    public DateTime RequestDateTime { get; set; }

    public DateTime? ValidationDateTime { get; set; }

    public DateTime? QbchTotalExecutionDateTime { get; set; }

    public DateTime? ResponseDateTime { get; set; }

    public string? RequestId { get; set; }

    public int? RequsetTypeKeyId { get; set; }

    public int? UserTypeId { get; set; }

    public long? UserId { get; set; }

    public string? RequestXml { get; set; }

    public byte[]? RequestSignedData { get; set; }

    public string? ErrorMessage { get; set; }

    public int? ErrorCodeKeyId { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

    
    public void Dispose()
    {
        
        // подавляем финализацию
        GC.SuppressFinalize(this);
    }
}
