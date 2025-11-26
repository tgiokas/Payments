using Payments.Application.Dtos;
using Payments.Domain.Entities;

namespace Payments.Application.Services;

public interface IPaymentService
{
    Task<PaymentResultDto> ConfirmByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default);
    Task<PaymentInitiateResponseDto> InitiateAsync(PaymentInitiateRequestDto req, string idempotencyKey, string? tenantKey, CancellationToken ct = default);
}