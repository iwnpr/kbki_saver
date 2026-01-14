using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TeSubject
{
    public long KeyId { get; set; }

    public long RequestKeyId { get; set; }

    public DateOnly? BirthDay { get; set; }

    public string? Inn { get; set; }

    public string? Psrn { get; set; }

    public string? Snils { get; set; }

}
