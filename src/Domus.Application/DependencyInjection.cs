using Domus.Application.Houses;
using Domus.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDomusApplication(this IServiceCollection services)
    {
        services.AddScoped<MeService>();
        services.AddScoped<HouseService>();
        return services;
    }
}

