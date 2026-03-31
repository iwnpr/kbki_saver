using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Список абонентов
/// </summary>
public partial class TrAbonent
{
    public int KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public int UserType { get; set; }

    public string FullName { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public string? Inn { get; set; }

    public string Ogrn { get; set; } = null!;

    public string? Email { get; set; }

    public virtual ICollection<TdPermission> TdPermissions { get; set; } = new List<TdPermission>();

    public virtual ICollection<TeCertManage> TeCertManages { get; set; } = new List<TeCertManage>();

    public virtual ICollection<TeDlanswer> TeDlanswers { get; set; } = new List<TeDlanswer>();

    public virtual ICollection<TeDlputanswer> TeDlputanswers { get; set; } = new List<TeDlputanswer>();

    public virtual ICollection<TeDlput> TeDlputs { get; set; } = new List<TeDlput>();

    public virtual ICollection<TeDlrequest> TeDlrequests { get; set; } = new List<TeDlrequest>();

    public virtual ICollection<TeQbchTask> TeQbchTasks { get; set; } = new List<TeQbchTask>();

    public virtual ICollection<TrAbonentCertificate> TrAbonentCertificates { get; set; } = new List<TrAbonentCertificate>();

    public virtual TrUserType UserTypeNavigation { get; set; } = null!;
}
