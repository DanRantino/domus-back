using System.Text.Json;
using Domus.Api.Features.Users;
using Domus.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
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

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing required configuration: ConnectionStrings:Database or DATABASE_URL.");
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<DomusDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMeEndpoints();

app.Run();

public partial class Program;
