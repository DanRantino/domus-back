using Domus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests.Support;

public sealed class DomusApiFactory : WebApplicationFactory<Program>
{
    private readonly string _sqliteConnection =
        $"Data Source=file:domus-{Guid.NewGuid():N}?mode=memory&cache=shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Authentication:Authority", "https://logto.test/oidc");
        builder.UseSetting("Authentication:Audience", "https://api.domus.test");
        builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=unused;Username=u;Password=p");

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DomusDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<DomusDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(DomusDbContext)))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // EF Core 8 also registers options configuration delegates keyed by context type.
            foreach (var descriptor in services
                         .Where(d => d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true
                                     && d.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(DomusDbContext))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DomusDbContext>(options => options.UseSqlite(_sqliteConnection));

            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
