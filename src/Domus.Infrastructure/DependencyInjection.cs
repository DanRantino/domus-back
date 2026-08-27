using Domus.Application.Houses;
using Domus.Application.Users;
using Domus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDomusInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<DomusDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(DomusDbContext).Assembly.FullName)));

        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<HouseMembershipReader>();
        services.AddScoped<IHouseMembershipReader>(sp =>
            sp.GetRequiredService<HouseMembershipReader>());
        services.AddScoped<IHouseWriter>(sp =>
            sp.GetRequiredService<HouseMembershipReader>());

        services.AddHealthChecks()
            .AddDbContextCheck<DomusDbContext>("database", tags: ["ready"]);

        return services;
    }
}
