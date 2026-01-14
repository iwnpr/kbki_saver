using System;
using System.Collections.Generic;

namespace db_lib.DBEntity;

public partial class TrAbonent
{
    public int KeyId { get; set; }

    public int UserTypeId { get; set; }

    public string FullName { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public string? Inn { get; set; }

    public string Ogrn { get; set; } = null!;


}
