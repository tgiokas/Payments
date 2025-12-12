using System.Text.Json.Serialization;

namespace Payments.Application.Dtos;

public class JccRegisterOrderResponse
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }
    [JsonPropertyName("formUrl")]
    public string? FormUrl { get; set; }
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}