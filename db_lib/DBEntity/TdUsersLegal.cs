using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TdUsersLegal
{
    public long KeyId { get; set; }

    public bool? IsForeign { get; set; }

    public string? Inn { get; set; }

    public string? Ogrn { get; set; }

    public string? FullName { get; set; }

    public string? ShortName { get; set; }

    public string? OtherName { get; set; }
}
