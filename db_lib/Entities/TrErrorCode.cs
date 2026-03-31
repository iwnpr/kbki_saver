using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Коды ошибок согласно документации
/// + добавленные коды 500 и 0
/// </summary>
public partial class TrErrorCode
{
    public int KeyId { get; set; }

    public string Description { get; set; } = null!;

    public string? Comments { get; set; }

    public virtual ICollection<TeDlanswer> TeDlanswers { get; set; } = new List<TeDlanswer>();

    public virtual ICollection<TeDlrequest> TeDlrequests { get; set; } = new List<TeDlrequest>();

    public virtual ICollection<TeQbchDlanswer> TeQbchDlanswers { get; set; } = new List<TeQbchDlanswer>();

    public virtual ICollection<TeQbchDlrequest> TeQbchDlrequests { get; set; } = new List<TeQbchDlrequest>();

    public virtual ICollection<TeRequest> TeRequests { get; set; } = new List<TeRequest>();
}
