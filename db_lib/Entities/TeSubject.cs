using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Субъект в запросе
/// </summary>
public partial class TeSubject
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public long RequestId { get; set; }

    public DateOnly? BirthDay { get; set; }

    public string? Inn { get; set; }

    public bool? InnChecked { get; set; }

    public bool? InnForeign { get; set; }

    public string? Psrn { get; set; }

    public string? Snils { get; set; }

    public virtual TeRequest Request { get; set; } = null!;

    public virtual ICollection<TeSubjectsDocument> TeSubjectsDocuments { get; set; } = new List<TeSubjectsDocument>();

    public virtual ICollection<TeSubjectsFullName> TeSubjectsFullNames { get; set; } = new List<TeSubjectsFullName>();
}
