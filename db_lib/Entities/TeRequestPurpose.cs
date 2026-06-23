namespace db_lib.Entities;

public partial class TeRequestPurpose
{
    public long KeyId { get; set; }

    public long RequestId { get; set; }

    public string? PurposeId { get; set; }

    public virtual TeRequest Request { get; set; } = null!;
}