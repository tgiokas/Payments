using Payments.Domain.Entities;

namespace Payments.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<Payment?> GetByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default);
    Task<Payment?> GetByIdempotencyAsync(string idempotencyKey, CancellationToken ct = default);

    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);    
}