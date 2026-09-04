using System.Net.Http.Headers;
using System.Text.Json;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;
using Domus.Domain.Users;
using Domus.Infrastructure.Persistence;
using Domus.Application.Houses;
using Microsoft.Extensions.DependencyInjection;

namespace Domus.Api.Tests.Support;

internal static class EndpointTestData
{
    public static JsonSerializerOptions SnakeCaseJson { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static HttpClient CreateAuthenticatedClient(
        this DomusApiFactory factory,
        string sub,
        string? email = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        }

        return client;
    }

    public static async Task<User> SeedUserAsync(
        this DomusApiFactory factory,
        string identityId,
        string? fullName = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();

        var user = new User(
            Guid.NewGuid(),
            identityId,
            fullName);

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

    public static async Task SeedMembershipAsync(
        this DomusApiFactory factory,
        Guid userId,
        Guid houseId,
        string role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        db.HouseMemberships.Add(new HouseMembership
        {
            UserId = userId,
            HouseId = houseId,
            Role = role,
        });
        await db.SaveChangesAsync();
    }

    public static async Task<HouseTask> SeedHouseTaskAsync(
        this DomusApiFactory factory,
        Guid houseId,
        Guid createdByUserId,
        string title,
        Guid? assigneeUserId = null,
        string? description = null,
        string status = HouseTaskStatuses.Pending,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? completedAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var now = DateTimeOffset.UtcNow;
        var task = new HouseTask(
            Guid.NewGuid(),
            houseId,
            title,
            createdByUserId,
            now,
            description,
            dueAt,
            assigneeUserId);

        if (status == HouseTaskStatuses.Completed)
        {
            task.Complete(completedAt ?? now);
        }

        db.HouseTasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public static int CountUsers(this DomusApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return db.Users.Count();
    }

    public static int CountHouses(this DomusApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return db.Houses.Count();
    }

    public static int CountInvitations(this DomusApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return db.HouseInvitations.Count();
    }

    public static int CountMemberships(this DomusApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        return db.HouseMemberships.Count();
    }

    public static async Task<HouseInvitation> SeedInvitationAsync(
        this DomusApiFactory factory,
        Guid houseId,
        Guid invitedByUserId,
        string email,
        string token,
        string role = HouseRoles.Member,
        string status = HouseInvitationStatuses.Pending,
        DateTimeOffset? expiresAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();
        var invitation = new HouseInvitation
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            InvitedByUserId = invitedByUserId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            TokenHash = InvitationTokens.Hash(token),
            Status = status,
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.HouseInvitations.Add(invitation);
        await db.SaveChangesAsync();
        return invitation;
    }
}
