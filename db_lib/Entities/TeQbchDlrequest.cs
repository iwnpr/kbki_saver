using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TeQbchDlrequest
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public DateTime? RequestDateTime { get; set; }

    public DateTime? ResponseDateTime { get; set; }

    public byte[]? RequestData { get; set; }

    public string? RequestXml { get; set; }

    public long? QbchTaskId { get; set; }

    public int ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int? HttpResponseCode { get; set; }

    public byte[]? ResponseData { get; set; }

    public string? ResponseXml { get; set; }

    public virtual TrErrorCode ErrorCodeNavigation { get; set; } = null!;

    public virtual TeQbchTask? QbchTask { get; set; }
}
