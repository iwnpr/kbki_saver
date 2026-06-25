using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Список пользователей - контрагентов. Здесь присутствуют как контрагенты КБКИ так и внешние, которые пришли к нам с запросом.
/// </summary>
public partial class TdUser
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public int UserType { get; set; }

    public bool IsForeign { get; set; }

    public string? Inn { get; set; }

    public string? Ogrn { get; set; }

    public string? FullName { get; set; }

    public string? ShortName { get; set; }

    public string? OtherName { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? BirthPlace { get; set; }

    public string? Snils { get; set; }

    public string? DocType { get; set; }

    public string? DocOtherName { get; set; }

    public string? DocSeria { get; set; }

    public string? DocNumber { get; set; }

    public DateOnly? DocIssueDate { get; set; }

    public string? DocIssuerName { get; set; }

    public string? DocIssuerCode { get; set; }

    //public string? UserTypeCodeId { get; set; }

    public virtual TrDocumentType? DocTypeNavigation { get; set; }

    public virtual ICollection<TeRequest> TeRequests { get; set; } = new List<TeRequest>();

    public virtual TrUserType UserTypeNavigation { get; set; } = null!;
}
