using System.Text.Json;
using Domus.Api.Http;
using Domus.Application;
using Domus.Infrastructure;
using Domus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Domus.Api.Configuration;
using Domus.Infrastructure.DevelopmentSeed;
using Domus.Infrastructure.Identity;

DotEnvLoader.Load();

var isSeed = args.Contains("--seed");

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
if (isSeed || builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.IncludeScopes = false;
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "O";
        options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    });
}

if (!isSeed)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    var authority = builder.Configuration["Authentication:Authority"];
    var audience = builder.Configuration["Authentication:Audience"];
    var clientId = builder.Configuration["Authentication:ClientId"];
    var clientSecret = builder.Configuration["Authentication:ClientSecret"];
    var connectionString = DatabaseConnection.Resolve(builder.Configuration);
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? ["http://localhost:3000"];

    if (string.IsNullOrWhiteSpace(authority))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Authentication:Authority (env Authentication__Authority).");
    }

    if (string.IsNullOrWhiteSpace(audience))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Authentication:Audience (env Authentication__Audience).");
    }

    if (string.IsNullOrWhiteSpace(clientId))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Authentication:ClientId (env Authentication__ClientId).");
    }

    if (string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Authentication:ClientSecret (env Authentication__ClientSecret).");
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Missing required configuration: ConnectionStrings:Database or DATABASE_URL.");
    }

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddDomusAuthentication(authority, audience, clientId, clientSecret);
    builder.Services.AddDomusApplication();
    builder.Services.AddDomusInfrastructure(connectionString);

    builder.Services.AddSwaggerGen();

}
if (isSeed)
{
    var connectionString = DatabaseConnection.Resolve(builder.Configuration);
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Missing required configuration: ConnectionStrings:Database or DATABASE_URL.");
    }

    builder.Services.AddDomusInfrastructure(connectionString);
    builder.Services.Configure<DevelopmentSeedOptions>(
        builder.Configuration.GetSection(
            DevelopmentSeedOptions.SectionName));
    builder.Services.AddHttpClient<LogtoManagementClient>();
    builder.Services.AddScoped<UserSeeder>();
    builder.Services.AddScoped<UserSeederDB>();
    builder.Services.AddScoped<HouseSeederDB>();
    builder.Services.AddScoped<HouseMembershipSeederDB>();
    builder.Services.AddScoped<AppSeed>();
}

var app = builder.Build();

if (isSeed)
{
    using var scope = app.Services.CreateScope();
    var appSeed = scope.ServiceProvider.GetRequiredService<AppSeed>();
    await appSeed.RunAsync();
    return;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
    if (db.Database.IsNpgsql())
    {
        db.Database.Migrate();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
}).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;
