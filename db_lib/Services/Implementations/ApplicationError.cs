using StackExchange.Redis;

namespace db_lib.Services.Implementations
{
    public partial class SaverService
    {
        public class ApplicationError()
        {
            public string ServiceName { get; set; } = null!;
            public string guid { get; set; } = null!;
            public string? Id { get; set; }
            public string? IpAddress { get; set; }
            public string? Thumbprint { get; set; }
            public string? RequestTime { get; set; }
            public string? ResponseTime { get; set; }
            public string? Message { get; set; }

            public HashEntry[] CastToHashEnrties()
            {
                return [
                    new("IpAddress",IpAddress),
                    new("guid",Id),
                    new("Thumbprint",Thumbprint),
                    new("RequestTime",RequestTime),
                    new("ResponseTime",ResponseTime),
                    new("ErrorMessage",Message),
                    new("ErrorCode",500)
                ];
            }
        }
    }
}