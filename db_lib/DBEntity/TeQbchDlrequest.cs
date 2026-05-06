using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeQbchDlrequest
{
    public long KeyId { get; set; }

    public long DlrequestMainKeyId { get; set; }

    public int QbchKeyId { get; set; }

    public DateTime TaskStartDateTime { get; set; }

    public DateTime? DlrequestStartDateTime { get; set; }

    public DateTime? DlanswerStartDateTime { get; set; }

    public DateTime? ResponseDateTime { get; set; }

    public string? RequestXml { get; set; }

    public byte[]? RequestSignedData { get; set; }

    public string? ErrorMessage { get; set; }

    public int? ErrorCodeKeyId { get; set; }

    public string? ResponseId { get; set; }

    public int DlrequestResendCount { get; set; }

    public int DlanswerResendCount { get; set; }

    public int? ResponseType { get; set; }

    public string? ResponseXml { get; set; }

    public byte[]? ResponseSignedData { get; set; }

}
