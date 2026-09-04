using System.Text.Json;
using Domus.Api.Http;
using Domus.Application;
using Domus.Application.Houses;
using Domus.Infrastructure;
using Domus.Infrastructure.Mail;
using Domus.Infrastructure.Persistence;
using Logto.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Domus.Api.Configuration;
using Domus.Api.GraphQL;
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
    var logtoEndpoint = builder.Configuration["Logto:Endpoint"];
    var logtoAppId = builder.Configuration["Logto:AppId"];
    var logtoAppSecret = builder.Configuration["Logto:AppSecret"];
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

    if (string.IsNullOrWhiteSpace(logtoEndpoint))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Logto:Endpoint (env Logto__Endpoint).");
    }

    if (string.IsNullOrWhiteSpace(logtoAppId))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Logto:AppId (env Logto__AppId).");
    }

    if (string.IsNullOrWhiteSpace(logtoAppSecret))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Logto:AppSecret (env Logto__AppSecret).");
    }

    if (!logtoEndpoint.EndsWith('/'))
    {
        logtoEndpoint += "/";
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

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddLogtoAuthentication(options =>
    {
        options.Endpoint = logtoEndpoint;
        options.AppId = logtoAppId;
        options.AppSecret = logtoAppSecret;
        options.GetClaimsFromUserInfoEndpoint = true;
        if (!options.Scopes.Contains(LogtoParameters.Scopes.Email))
        {
            options.Scopes.Add(LogtoParameters.Scopes.Email);
        }
        // Cookie BFF authenticates the SPA from the session cookie, not a resource
        // access token. Setting Resource makes the Logto SDK reject the principal
        // when access_token.resource is missing, which loops /dashboard ↔ /oidc.
    });

    builder.Services
        .AddAuthentication()
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "sub",
            };
        })
        .AddPolicyScheme(
            DomusAuthSchemes.CookieOrBearer,
            DomusAuthSchemes.CookieOrBearer,
            options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers.Authorization.ToString();
                    return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : LogtoDefaults.CookieScheme;
                };
            });

    builder.Services.PostConfigure<AuthenticationOptions>(options =>
    {
        options.DefaultAuthenticateScheme = DomusAuthSchemes.CookieOrBearer;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = LogtoDefaults.AuthenticationScheme;
    });

    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        LogtoDefaults.CookieScheme,
        options =>
        {
            options.Cookie.HttpOnly = true;
            // form_post from the IdP origin; Lax is dropped and login loops.
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

    builder.Services.AddAuthorization();
    builder.Services.AddHttpContextAccessor();
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>();
    builder.Services.AddDomusApplication();
    builder.Services.AddDomusInfrastructure(connectionString);

    builder.Services.Configure<ResendOptions>(
        builder.Configuration.GetSection(ResendOptions.SectionName));
    builder.Services.Configure<InvitationMailOptions>(
        builder.Configuration.GetSection(InvitationMailOptions.SectionName));
    builder.Services.Configure<IdentityEmailOptions>(options =>
    {
        options.Authority = authority;
    });
    builder.Services.AddHttpClient(nameof(IdentityEmailResolver));
    builder.Services.AddScoped<IdentityEmailResolver>();
    builder.Services.AddScoped<LoggingInvitationMailer>();
    builder.Services.AddHttpClient<ResendInvitationMailer>();
    if (!builder.Environment.IsDevelopment()
        && string.IsNullOrWhiteSpace(builder.Configuration["Resend:ApiKey"]))
    {
        throw new InvalidOperationException(
            "Missing required configuration: Resend:ApiKey (env Resend__ApiKey).");
    }

    builder.Services.AddScoped<IInvitationMailer>(sp =>
    {
        var apiKey = sp.GetRequiredService<IOptions<ResendOptions>>().Value.ApiKey;
        return string.IsNullOrWhiteSpace(apiKey)
            ? sp.GetRequiredService<LoggingInvitationMailer>()
            : sp.GetRequiredService<ResendInvitationMailer>();
    });

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
    builder.Services.AddScoped<HouseTaskSeederDB>();
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
app.UseMiddleware<CurrentUserMiddleware>();

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
app.MapGraphQL("/graphql")
    .RequireAuthorization()
    .WithOptions(options =>
    {
        options.Tool.Enable = app.Environment.IsDevelopment();
    });

app.Run();

public partial class Program;
