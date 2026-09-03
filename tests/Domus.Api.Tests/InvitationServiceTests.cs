using Domus.Application.Houses;
using Domus.Domain.Houses;

namespace Domus.Api.Tests;

public sealed class InvitationServiceTests
{
    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HouseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_DefaultsRoleToMember_AndHashesToken()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "  Guest@Example.com ",
            role: null,
            CancellationToken.None);

        Assert.True(result.IsCreated);
        Assert.Equal("guest@example.com", result.Value!.Email);
        Assert.Equal(HouseRoles.Member, result.Value.Role);
        Assert.Equal(HouseInvitationStatuses.Pending, result.Value.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.True(result.Value.EmailSent);
        var stored = Assert.Single(fixture.Store.Items);
        Assert.Equal(InvitationTokens.Hash(result.Value.Token!), stored.TokenHash);
        Assert.Equal("guest@example.com", stored.Email);
        var sent = Assert.Single(fixture.Mailer.Sent);
        Assert.Equal("guest@example.com", sent.To);
        Assert.Equal(result.Value.Token, sent.Token);
        Assert.Equal("Casa Centro", sent.HouseName);
    }

    [Fact]
    public async Task Create_AllowsAdminRole()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            HouseRoles.Admin,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HouseRoles.Admin, result.Value!.Role);
    }

    [Fact]
    public async Task Create_RejectsGuestRole()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            HouseRoles.Guest,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_error", result.Error!.Code);
        Assert.Empty(fixture.Store.Items);
    }

    [Fact]
    public async Task Create_NonAdmin_IsForbidden()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            MemberId,
            "Bob",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error!.Code);
        Assert.Empty(fixture.Store.Items);
    }

    [Fact]
    public async Task Create_DuplicatePending_IsConflict()
    {
        var fixture = CreateFixture();
        await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        var result = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "GUEST@example.com",
            role: null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("conflict", result.Error!.Code);
        Assert.Single(fixture.Store.Items);
    }

    [Fact]
    public async Task Create_MailerFailure_LeavesPendingInvitation()
    {
        var fixture = CreateFixture();
        fixture.Mailer.Fail = true;

        var result = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        Assert.True(result.IsCreated);
        Assert.False(result.Value!.EmailSent);
        Assert.Single(fixture.Store.Items);
        Assert.Equal(HouseInvitationStatuses.Pending, fixture.Store.Items[0].Status);
    }

    [Fact]
    public async Task Accept_MatchingEmail_CreatesMembership()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);
        var inviteeId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var result = await fixture.Service.AcceptAsync(
            inviteeId,
            created.Value!.Token,
            "Guest@Example.com",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HouseId, result.Value!.HouseId);
        Assert.Equal(HouseRoles.Member, result.Value.Role);
        Assert.Equal("Casa Centro", result.Value.HouseName);
        Assert.Contains(
            fixture.Writer.Members,
            item => item.UserId == inviteeId && item.HouseId == HouseId);
        Assert.Equal(HouseInvitationStatuses.Accepted, fixture.Store.Items[0].Status);
    }

    [Fact]
    public async Task Accept_MismatchedEmail_IsForbidden()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        var result = await fixture.Service.AcceptAsync(
            Guid.NewGuid(),
            created.Value!.Token,
            "other@example.com",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error!.Code);
        Assert.Equal("Forbidden", result.Error.Message);
        Assert.Empty(fixture.Writer.Members);
        Assert.Equal(HouseInvitationStatuses.Pending, fixture.Store.Items[0].Status);
    }

    [Fact]
    public async Task Accept_Expired_IsNotFound()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);
        fixture.Time.UtcNow = Now.AddDays(8);

        var result = await fixture.Service.AcceptAsync(
            Guid.NewGuid(),
            created.Value!.Token,
            "guest@example.com",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Empty(fixture.Writer.Members);
    }

    [Fact]
    public async Task Accept_ReusedToken_IsDenied()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);
        var inviteeId = Guid.NewGuid();
        await fixture.Service.AcceptAsync(
            inviteeId,
            created.Value!.Token,
            "guest@example.com",
            CancellationToken.None);
        fixture.Memberships.ByUser[inviteeId] =
        [
            new HouseMembershipSummary(HouseId, "Casa Centro", HouseRoles.Member),
        ];

        var result = await fixture.Service.AcceptAsync(
            inviteeId,
            created.Value.Token,
            "guest@example.com",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Accept_AlreadyMember_IsConflict()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        var result = await fixture.Service.AcceptAsync(
            AdminId,
            created.Value!.Token,
            "guest@example.com",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("conflict", result.Error!.Code);
        Assert.Empty(fixture.Writer.Members);
        Assert.Equal(HouseInvitationStatuses.Pending, fixture.Store.Items[0].Status);
    }

    [Fact]
    public async Task Preview_ValidToken_ReturnsHouseNameOnly()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);

        var result = await fixture.Service.PreviewAsync(
            created.Value!.Token,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Casa Centro", result.Value!.HouseName);
    }

    [Fact]
    public async Task Preview_UnknownToken_IsNotFound()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.PreviewAsync("unknown-token", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Resend_RotatesToken()
    {
        var fixture = CreateFixture();
        var created = await fixture.Service.CreateAsync(
            AdminId,
            "Ada",
            HouseId,
            "guest@example.com",
            role: null,
            CancellationToken.None);
        var original = created.Value!.Token;

        var resent = await fixture.Service.ResendAsync(
            AdminId,
            "Ada",
            HouseId,
            created.Value.Id,
            CancellationToken.None);

        Assert.True(resent.IsSuccess);
        Assert.NotEqual(original, resent.Value!.Token);
        var oldAccept = await fixture.Service.AcceptAsync(
            Guid.NewGuid(),
            original,
            "guest@example.com",
            CancellationToken.None);
        Assert.Equal("not_found", oldAccept.Error!.Code);
    }

    private static Fixture CreateFixture()
    {
        var store = new FakeInvitationStore();
        var memberships = new FakeMembershipReader();
        memberships.ByUser[AdminId] =
        [
            new HouseMembershipSummary(HouseId, "Casa Centro", HouseRoles.Admin),
        ];
        memberships.ByUser[MemberId] =
        [
            new HouseMembershipSummary(HouseId, "Casa Centro", HouseRoles.Member),
        ];
        var writer = new FakeHouseWriter();
        var mailer = new FakeMailer();
        var time = new FixedTimeProvider { UtcNow = Now };
        return new Fixture(
            new InvitationService(store, memberships, writer, mailer, time),
            store,
            memberships,
            writer,
            mailer,
            time);
    }

    private sealed record Fixture(
        InvitationService Service,
        FakeInvitationStore Store,
        FakeMembershipReader Memberships,
        FakeHouseWriter Writer,
        FakeMailer Mailer,
        FixedTimeProvider Time);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class FakeInvitationStore : IHouseInvitationStore
    {
        public List<HouseInvitation> Items { get; } = [];

        public Task<HouseInvitation?> FindByIdAsync(
            Guid houseId,
            Guid invitationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == invitationId && i.HouseId == houseId));

        public Task<HouseInvitation?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.TokenHash == tokenHash));

        public Task<HouseInvitation?> FindPendingByHouseAndEmailAsync(
            Guid houseId,
            string email,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i =>
                i.HouseId == houseId
                && i.Email == email
                && i.Status == HouseInvitationStatuses.Pending));

        public Task<IReadOnlyList<HouseInvitation>> ListPendingByHouseIdAsync(
            Guid houseId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HouseInvitation>>(
                Items
                    .Where(i =>
                        i.HouseId == houseId
                        && i.Status == HouseInvitationStatuses.Pending
                        && i.ExpiresAt > now)
                    .ToArray());

        public Task<int> CountPendingByHouseIdAsync(
            Guid houseId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Count(i =>
                i.HouseId == houseId
                && i.Status == HouseInvitationStatuses.Pending
                && i.ExpiresAt > now));

        public Task AddAsync(HouseInvitation invitation, CancellationToken cancellationToken)
        {
            invitation.House = new House { Id = invitation.HouseId, Name = "Casa Centro" };
            Items.Add(invitation);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMembershipReader : IHouseMembershipReader
    {
        public Dictionary<Guid, List<HouseMembershipSummary>> ByUser { get; } = [];

        public Task<IReadOnlyList<HouseMembershipSummary>> ListByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (ByUser.TryGetValue(userId, out var houses))
            {
                return Task.FromResult<IReadOnlyList<HouseMembershipSummary>>(houses);
            }

            return Task.FromResult<IReadOnlyList<HouseMembershipSummary>>([]);
        }
    }

    private sealed class FakeHouseWriter : IHouseWriter
    {
        public List<(Guid UserId, Guid HouseId, string Role)> Members { get; } = [];

        public Task<HouseMembershipSummary> CreateWithOwnerAsync(
            Guid userId,
            string name,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddMemberAsync(
            Guid userId,
            Guid houseId,
            string role,
            CancellationToken cancellationToken)
        {
            Members.Add((userId, houseId, role));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMailer : IInvitationMailer
    {
        public bool Fail { get; set; }

        public List<InvitationEmail> Sent { get; } = [];

        public Task<bool> SendAsync(InvitationEmail email, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                return Task.FromResult(false);
            }

            Sent.Add(email);
            return Task.FromResult(true);
        }
    }
}
