using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Задача в рамках которой отправляются запросы во внешние КБКИ. (Сбор данных из нашей БД так же приравнивается к внешней КБКИ)
/// </summary>
public partial class TeQbchTask
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public DateTime? TaskStartDateTime { get; set; }

    public DateTime? TaskEndDateTime { get; set; }

    public long ReqId { get; set; }

    public int QbchCorrespondentId { get; set; }

    public string? ResponseId { get; set; }

    public string? TaskResultXml { get; set; }

    public virtual TrAbonent QbchCorrespondent { get; set; } = null!;

    public virtual TeDlrequest Req { get; set; } = null!;

    public virtual ICollection<TeQbchDlanswer> TeQbchDlanswers { get; set; } = new List<TeQbchDlanswer>();

    public virtual ICollection<TeQbchDlrequest> TeQbchDlrequests { get; set; } = new List<TeQbchDlrequest>();

    public virtual ICollection<TeResponse> TeResponses { get; set; } = new List<TeResponse>();
}
