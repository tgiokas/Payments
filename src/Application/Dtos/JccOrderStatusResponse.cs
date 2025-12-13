using System.Text.Json.Serialization;

namespace Payments.Application.Dtos;

public class JccOrderStatusResponse
{
    [JsonPropertyName("orderStatus")]
    public int? OrderStatus { get; set; }
    [JsonPropertyName("actionCode")]
    public int? ActionCode { get; set; }
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}