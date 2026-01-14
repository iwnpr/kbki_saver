namespace db_lib.Entity.CommonTypes.Api
{
    public class QBCHResult : BaseResult
    {
        public string? QBCHPSRN { get; set; }
        public byte[] Body { get; set; } = null!;
    }
}
