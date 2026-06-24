namespace db_lib.Entities;

public partial class TeConsentPurpose
{
    public long KeyId { get; set; }

    public long RequestId { get; set; }

    public int? PurposeId { get; set; }

    public virtual TeRequest Request { get; set; } = null!;
}
