using Payments.Application.Interfaces;
//using Payments.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Payments.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {       
        //services.AddScoped<IPaymentsService, PaymentsService>();
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

