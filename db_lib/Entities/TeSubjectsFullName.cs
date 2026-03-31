using System;
using System.Collections.Generic;

namespace db_lib.Entities;

public partial class TeSubjectsFullName
{
    public long KeyId { get; set; }

    public DateTime? Inserted { get; set; }

    public long SubjectId { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public virtual TeSubject Subject { get; set; } = null!;
}
