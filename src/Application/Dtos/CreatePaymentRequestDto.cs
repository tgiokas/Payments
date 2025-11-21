namespace Payments.Application.Dtos;

public record CreatePaymentRequestDto(
    decimal Amount,
    string Currency,
    string OrderNumber,
    string Method // "card" | "transit"
);