using System.Text.Json.Serialization;

namespace QBCHService_lib.Models.DTOs;

public class RedisMessageDTO
{
    [JsonPropertyName("request_date_time")] public string? RequestDateTime { get; set; }
    [JsonPropertyName("response_date_time")] public string? ResponseDateTime { get; set; }
    [JsonPropertyName("request_data")] public byte[]? RequestData { get; set; }
    [JsonPropertyName("request_xml")] public byte[]? RequestXml { get; set; }
    [JsonPropertyName("error_code")] public string? ErrorCode { get; set; }
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }
    [JsonPropertyName("http_response_code")] public string? HttpResponseCode { get; set; }
    [JsonPropertyName("response_data")] public byte[]? ResponseData { get; set; }
    [JsonPropertyName("response_xml")] public byte[]? ResponseXml { get; set; }
}
