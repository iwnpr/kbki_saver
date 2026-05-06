using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeSubjectsFullName
{
    public long KeyId { get; set; }

    public long SubjectKeyId { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

}
