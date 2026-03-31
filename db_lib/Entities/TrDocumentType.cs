using System;
using System.Collections.Generic;

namespace db_lib.Entities;

/// <summary>
/// Типы документов
/// </summary>
public partial class TrDocumentType
{
    public string KeyId { get; set; } = null!;

    public string DocDescription { get; set; } = null!;

    public virtual ICollection<TdUser> TdUsers { get; set; } = new List<TdUser>();

    public virtual ICollection<TeSubjectsDocument> TeSubjectsDocuments { get; set; } = new List<TeSubjectsDocument>();
}
