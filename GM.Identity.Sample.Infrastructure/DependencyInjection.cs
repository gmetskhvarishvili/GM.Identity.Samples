using GM.HttpClient;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Infrastructure.Options;
using GM.Identity.Sample.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Infrastructure.Services.OTP;
using GM.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Identity.Sample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OAuthOptions>(configuration.GetSection("OAuth"));
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IOTPService, OTPService>();
        services.AddGMHttpClient<IOTPAPIService, GMAPIClientOptions>(
            configuration.GetSection("ApiServices:OTPAPIService"),
            "OTPAPIService");
        return services;
    }
    
    public static IServiceCollection AddProducerWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGMMessaging(configuration);
        return services;
    }
}