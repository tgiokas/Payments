using Payments.Domain.Entities;

namespace Payments.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> FindByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<Payment?> FindByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default);
    Task<Payment?> FindByIdempotencyAsync(string idempotencyKey, string? tenantKey, CancellationToken ct = default);

    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);    
}