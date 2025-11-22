using Payments.Application.Interfaces;
//using Payments.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Services;

namespace Payments.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {       
        services.AddScoped<PaymentAppService, PaymentAppService>();
        //services.AddScoped<IAuthorizationService, AuthorizationService>();
        //services.AddScoped<IUserManagementService, UserManagementService>();
        //services.AddScoped<IRoleManagementService, RoleManagementService>();
        //services.AddScoped<IMfaService, MfaService>();
        //services.AddScoped<IPasswordResetService, PasswordResetService>();
        //services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        //services.AddScoped<ISmsVerificationService, SmsVerificationService>();
        //services.AddScoped<RolePermissionService, RolePermissionService>();

        return services;
    }
}

