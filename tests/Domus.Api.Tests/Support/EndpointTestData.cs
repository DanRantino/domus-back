using System.Net.Http.Headers;
using System.Text.Json;
using Domus.Domain.Houses;
using Domus.Domain.Users;
using Domus.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests.Support;

internal static class EndpointTestData
{
    public static JsonSerializerOptions SnakeCaseJson { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static HttpClient CreateAuthenticatedClient(this DomusApiFactory factory, string sub)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        return client;
    }

    public static async Task<User> SeedUserAsync(
        this DomusApiFactory factory,
        string identityId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();

        var user = new User(
            Guid.NewGuid(),
            identityId,
            null);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public static async Task<House> SeedHouseWithMembershipAsync(
        this DomusApiFactory factory,
        Guid userId,
        string name,
        string role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var house = new House { Id = Guid.NewGuid(), Name = name };
        db.Houses.Add(house);
        db.HouseMemberships.Add(new HouseMembership
        {
            UserId = userId,
            HouseId = house.Id,
            Role = role,
        });
        await db.SaveChangesAsync();
        return house;
    }

    public static int CountUsers(this DomusApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return db.Users.Count();
    }
}
