using Microsoft.EntityFrameworkCore;
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
        var connectionString = configuration["JCC_DB_CONNECTION"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database connection string 'JCC_DB_CONNECTION' is not configured.");        

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            switch (databaseProvider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":
                    // Pin history table to "public" to avoid schema mismatch (e.g. Zalando default schema "data")
                    // and race when multiple replicas run Migrate() at startup.
                    options.UseNpgsql(connectionString, npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    }).UseSnakeCaseNamingConvention();
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
