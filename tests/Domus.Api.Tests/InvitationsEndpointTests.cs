using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domus.Api.Contracts.Houses;
using Domus.Api.Http;
using Domus.Api.Tests.Support;
using Domus.Domain.Houses;

namespace Domus.Api.Tests;

public sealed class InvitationsEndpointTests : IAsyncLifetime
{
    private readonly DomusApiFactory _factory = new();
    private readonly JsonSerializerOptions _jsonOptions = EndpointTestData.SnakeCaseJson;

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateInvitation_Admin_Returns201()
    {
        const string identityId = "identity-admin-invite";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("guest@example.com", null),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<InvitationResponse>>(_jsonOptions);
        Assert.NotNull(body?.Data);
        Assert.Equal("guest@example.com", body.Data.Email);
        Assert.Equal("member", body.Data.Role);
        Assert.Equal("pending", body.Data.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.Data.Token));
        Assert.True(body.Data.EmailSent);
        Assert.Equal(1, _factory.CountInvitations());
    }

    [Fact]
    public async Task CreateInvitation_NonAdmin_Returns403()
    {
        const string identityId = "identity-member-invite";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Member);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("guest@example.com", null),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<InvitationResponse>>(_jsonOptions);
        Assert.Equal("forbidden", body!.Error!.Code);
        Assert.Equal(0, _factory.CountInvitations());
    }

    [Fact]
    public async Task CreateInvitation_DuplicatePending_Returns409()
    {
        const string identityId = "identity-admin-dup";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);
        await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("guest@example.com", null),
            _jsonOptions);

        var response = await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("GUEST@example.com", null),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<InvitationResponse>>(_jsonOptions);
        Assert.Equal("conflict", body!.Error!.Code);
        Assert.Equal(1, _factory.CountInvitations());
    }

    [Fact]
    public async Task CreateInvitation_GuestRole_Returns400()
    {
        const string identityId = "identity-admin-guest-role";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);

        var response = await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("guest@example.com", "guest"),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<InvitationResponse>>(_jsonOptions);
        Assert.Equal("validation_error", body!.Error!.Code);
        Assert.Equal(0, _factory.CountInvitations());
    }

    [Fact]
    public async Task Preview_ValidToken_HidesInviteeEmail()
    {
        const string identityId = "identity-admin-preview";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        const string token = "preview-token-value-xx";
        await _factory.SeedInvitationAsync(house.Id, user.Id, "secret@example.com", token);
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/invitations/preview?token={token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret@example.com", json, StringComparison.OrdinalIgnoreCase);
        var body = JsonSerializer.Deserialize<ApiEnvelope<InvitationPreviewResponse>>(json, _jsonOptions);
        Assert.Equal("Casa Centro", body!.Data!.HouseName);
    }

    [Fact]
    public async Task Preview_InvalidToken_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/invitations/preview?token=unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_MatchingEmail_JoinsHouse()
    {
        const string adminId = "identity-admin-accept";
        var admin = await _factory.SeedUserAsync(adminId);
        var house = await _factory.SeedHouseWithMembershipAsync(admin.Id, "Casa Centro", HouseRoles.Admin);
        const string token = "accept-token-value-xx";
        await _factory.SeedInvitationAsync(house.Id, admin.Id, "guest@example.com", token);

        const string guestId = "identity-guest-accept";
        await _factory.SeedUserAsync(guestId);
        var client = _factory.CreateAuthenticatedClient(guestId, "guest@example.com");

        var response = await client.PostAsJsonAsync(
            "/invitations/accept",
            new AcceptInvitationRequest(token),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<AcceptInvitationResponse>>(_jsonOptions);
        Assert.Equal(house.Id, body!.Data!.HouseId);
        Assert.Equal("member", body.Data.Role);
        Assert.Equal(2, _factory.CountMemberships());
    }

    [Fact]
    public async Task Accept_MismatchedEmail_Returns403()
    {
        const string adminId = "identity-admin-mismatch";
        var admin = await _factory.SeedUserAsync(adminId);
        var house = await _factory.SeedHouseWithMembershipAsync(admin.Id, "Casa Centro", HouseRoles.Admin);
        const string token = "mismatch-token-value-x";
        await _factory.SeedInvitationAsync(house.Id, admin.Id, "guest@example.com", token);

        const string otherId = "identity-other-mismatch";
        await _factory.SeedUserAsync(otherId);
        var client = _factory.CreateAuthenticatedClient(otherId, "other@example.com");

        var response = await client.PostAsJsonAsync(
            "/invitations/accept",
            new AcceptInvitationRequest(token),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("guest@example.com", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _factory.CountMemberships());
    }

    [Fact]
    public async Task Accept_Unprovisioned_Returns403()
    {
        const string adminId = "identity-admin-unprov";
        var admin = await _factory.SeedUserAsync(adminId);
        var house = await _factory.SeedHouseWithMembershipAsync(admin.Id, "Casa Centro", HouseRoles.Admin);
        const string token = "unprov-token-value-xxx";
        await _factory.SeedInvitationAsync(house.Id, admin.Id, "guest@example.com", token);
        var client = _factory.CreateAuthenticatedClient("identity-unprovisioned", "guest@example.com");

        var response = await client.PostAsJsonAsync(
            "/invitations/accept",
            new AcceptInvitationRequest(token),
            _jsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<AcceptInvitationResponse>>(_jsonOptions);
        Assert.Equal("not_provisioned", body!.Error!.Code);
        Assert.Equal(1, _factory.CountMemberships());
    }

    [Fact]
    public async Task ListInvitations_Admin_ReturnsPending()
    {
        const string identityId = "identity-admin-list";
        var user = await _factory.SeedUserAsync(identityId);
        var house = await _factory.SeedHouseWithMembershipAsync(user.Id, "Casa Centro", HouseRoles.Admin);
        var client = _factory.CreateAuthenticatedClient(identityId);
        await client.PostAsJsonAsync(
            $"/houses/{house.Id}/invitations",
            new CreateInvitationRequest("guest@example.com", "admin"),
            _jsonOptions);

        var response = await client.GetAsync($"/houses/{house.Id}/invitations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<InvitationResponse>>>(_jsonOptions);
        var invitation = Assert.Single(body!.Data!);
        Assert.Equal("guest@example.com", invitation.Email);
        Assert.Equal("admin", invitation.Role);
        Assert.Null(invitation.Token);
    }
}
