using Microsoft.EntityFrameworkCore;

using Payments.Domain.Entities;
using Payments.Domain.Interfaces;
using Payments.Infrastructure.Database;

namespace Payments.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _dbContext.Payments.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Payment?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return _dbContext.Payments.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, ct);
    }

    public Task<Payment?> GetByGatewayOrderIdAsync(string gatewayOrderId, CancellationToken ct = default)
    {
        return _dbContext.Payments.FirstOrDefaultAsync(x => x.GatewayOrderId == gatewayOrderId, ct);
    }

    public Task<Payment?> GetByIdempotencyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return _dbContext.Payments.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task AddAsync(Payment rule, CancellationToken ct = default)
    {
        await _dbContext.Payments.AddAsync(rule, ct);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment rule, CancellationToken ct = default)
    {
        _dbContext.Payments.Update(rule);
        await _dbContext.SaveChangesAsync();
    }    
}