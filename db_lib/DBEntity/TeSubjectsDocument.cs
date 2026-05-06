using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeSubjectsDocument
{
    public long KeyId { get; set; }

    public long SubjectKeyId { get; set; }

    public string DocTypeKeyId { get; set; } = null!;

    public string? DocSeries { get; set; }

    public string DocNumber { get; set; } = null!;

    public DateOnly DocDateIssue { get; set; }

    public int? CountryCode { get; set; }

}
