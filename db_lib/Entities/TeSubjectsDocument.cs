using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TeSubjectsDocument
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public long SubjectId { get; set; }

    public string DocTypeId { get; set; } = null!;

    public string? DocSeries { get; set; }

    public string DocNumber { get; set; } = null!;

    public DateOnly DocDateIssue { get; set; }

    public int? CountryCode { get; set; }

    public virtual TrDocumentType DocType { get; set; } = null!;

    public virtual TeSubject Subject { get; set; } = null!;
}
