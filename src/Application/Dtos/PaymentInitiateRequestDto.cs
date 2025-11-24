namespace Payments.Application.Dtos;

public record PaymentInitiateRequestDto(
    decimal Amount,
    string Currency,
    string OrderNumber,
    string Method // "card" | "transit"
);