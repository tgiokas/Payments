using Payments.Application.Dtos;

namespace Payments.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentInitiateResponseDto>> InitiateAsync(PaymentInitiateRequestDto req, string idempotencyKey, CancellationToken ct = default);
    Task<Result<PaymentResultDto>> ConfirmByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default);

    //Task<IReadOnlyList<Payment>> GetListAsync(PaymentStatus? status = null, CancellationToken ct = default);
}