using Microsoft.EntityFrameworkCore;

using Payments.Domain.Entities;
using Payments.Application.Interfaces;

namespace Payments.Infrastructure.Database;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    public required DbSet<Payment> Payments { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(b =>
        {           
            b.HasIndex(x => new { x.IdempotencyKey}).IsUnique();
            b.HasIndex(x => x.GatewayOrderId);
            b.HasIndex(x => x.OrderNumber).IsUnique();
        });
    }
}
