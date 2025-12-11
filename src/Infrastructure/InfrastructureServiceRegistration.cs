using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Payments.Domain.Interfaces;
using Payments.Infrastructure.Database;
using Payments.Infrastructure.Repositories;

namespace Payments.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration, string databaseProvider)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            switch (databaseProvider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":                    
                    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
                    break;

                case "sqlite":
                    //options.UseSqlite(connectionString);                     
                    break;

                default:
                    throw new ArgumentException($"Unsupported database provider: {databaseProvider}");
            }
        });

        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
}
