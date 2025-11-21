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

    public Task<Payment?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Payments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Payment?> FindByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => _dbContext.Payments.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, ct);

    public Task<Payment?> FindByIdempotencyAsync(string idempotencyKey, string? tenantKey, CancellationToken ct = default)
        => _dbContext.Payments.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.TenantKey == tenantKey, ct);


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

    //public async Task DeleteAsync(Payment rule)
    //{
    //    _dbContext.RolePermissions.Remove(rule);
    //    await _dbContext.SaveChangesAsync();
    //}
}