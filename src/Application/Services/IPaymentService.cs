using Payments.Application.Dtos;
using Payments.Domain.Entities;
using Payments.Domain.Enums;

namespace Payments.Application.Services;

public interface IPaymentService
{
    Task<PaymentInitiateResponseDto> InitiateAsync(PaymentInitiateRequestDto req, string idempotencyKey, CancellationToken ct = default);
    Task<PaymentResultDto> ConfirmByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default);
    
    //Task<IReadOnlyList<Payment>> GetListAsync(PaymentStatus? status = null, CancellationToken ct = default);
}