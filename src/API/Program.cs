using Microsoft.EntityFrameworkCore;

using Serilog;

using Payments.Api.Middlewares;
using Payments.Application;
using Payments.Application.Errors;
using Payments.Application.Interfaces;
using Payments.Infrastructure;
using Payments.Infrastructure.Database;
using Payments.Infrastructure.Messaging;
using Payments.Infrastructure.Gateways;
using Payments.Application.Services;


var builder = WebApplication.CreateBuilder(args);

// Register health check services
builder.Services.AddHealthChecks();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information("Configuration is starting...");

builder.Host.UseSerilog();

// Add memory cache
builder.Services.AddMemoryCache();

//builder.Services.AddScoped<IEmailCache, EmailCache>();
//builder.Services.AddScoped<ISmsCache, SmsCache>();

builder.Services.AddHttpClient<IJccRedirectGateway, JccRedirectGateway>();

// Add Application services
builder.Services.AddScoped<PaymentService, PaymentService>();

// Register Database Context
builder.Services.AddInfrastructureServices(builder.Configuration, "postgresql");

// Register Kafka-based Email sender
builder.Services.AddSingleton<IEmailSender, KafkaEmailSender>();

builder.Services.AddSingleton<IMessagePublisher, KafkaPublisher>();

//builder.Services.AddDistributedPostgreSqlCache(options =>
//{
//    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//    options.SchemaName = "public";
//    options.TableName = "CacheEntries";
//});

// Error Catalog Path
var path = Path.Combine(builder.Environment.ContentRootPath, "errors.json");
if (!File.Exists(path))
    throw new FileNotFoundException($"errors.json not found at: {path}");
Log.Information("Using error catalog at: {Path}", path);
var errorcat = ErrorCatalog.LoadFromFile(path);
builder.Services.AddSingleton<IErrorCatalog>(errorcat);

builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin();
        policyBuilder.AllowAnyMethod();
        policyBuilder.AllowAnyHeader();
    });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseRouting();

// Expose a simple health endpoint at /health
app.MapHealthChecks("/health");

Log.Information("Application is starting...");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
dbContext.Database.Migrate();
Log.Information("Database migrations applied (if any).");

app.UseCors("CorsPolicy");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<LogMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();