using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TdUsersIndividual
{
    public long KeyId { get; set; }

    public string? Inn { get; set; }

    public string? Ogrn { get; set; }

    public string? Snils { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string DocTypeKeyId { get; set; } = null!;

    public string? DocOtherName { get; set; }

    public string? DocSeria { get; set; }

    public string DocNumber { get; set; } = null!;

    public DateOnly DocIssueDate { get; set; }

    public string DocIssuerName { get; set; } = null!;

    public string? DocIssuerCode { get; set; }

    public DateOnly BirthDate { get; set; }

    public string BirthPlace { get; set; } = null!;

    
}
