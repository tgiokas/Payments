using Payments.Application.Dtos;

namespace Payments.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentInitiateResponse>> InitiatePaymentAsync(PaymentInitiateRequest req, string idempotencyKey, CancellationToken ct = default);
    Task<Result<PaymentConfirmResponse>> ConfirmPaymentAsync(string gatewayOrderId, CancellationToken ct = default);

    //Task<IReadOnlyList<Payment>> GetListAsync(PaymentStatus? status = null, CancellationToken ct = default);
}