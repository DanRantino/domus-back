using Domus.Application.Houses;
using Domus.Application.Tasks;
using Domus.Domain.Houses;
using Domus.Domain.Tasks;

namespace Domus.Api.Tests;

public sealed class HouseTaskServiceTests
{
    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HouseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt = new(2026, 9, 12, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Complete_Member_MarksPendingTaskCompleted()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteAsync(
            MemberId,
            HouseId,
            TaskId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HouseTaskStatuses.Completed, result.Value!.Status);
        Assert.Equal(CompletedAt, result.Value.CompletedAt);
        Assert.Equal(HouseTaskStatuses.Completed, fixture.Tasks.Items[0].Status);
        Assert.True(fixture.Tasks.Saved);
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_IsIdempotent()
    {
        var fixture = CreateFixture();
        fixture.Tasks.Items[0].Complete(CreatedAt.AddHours(1));
        fixture.Tasks.Saved = false;

        var result = await fixture.Service.CompleteAsync(
            MemberId,
            HouseId,
            TaskId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HouseTaskStatuses.Completed, result.Value!.Status);
        Assert.Equal(CreatedAt.AddHours(1), result.Value.CompletedAt);
        Assert.False(fixture.Tasks.Saved);
    }

    [Fact]
    public async Task Complete_UnknownHouse_IsNotFound()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteAsync(
            MemberId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            TaskId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Equal("House not found", result.Error.Message);
        Assert.False(fixture.Tasks.Saved);
    }

    [Fact]
    public async Task Complete_UnknownTask_IsNotFound()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteAsync(
            MemberId,
            HouseId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Equal("Task not found", result.Error.Message);
        Assert.False(fixture.Tasks.Saved);
    }

    [Fact]
    public async Task Complete_NonMember_IsNotFound()
    {
        var fixture = CreateFixture();
        var outsiderId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        var result = await fixture.Service.CompleteAsync(
            outsiderId,
            HouseId,
            TaskId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Equal("House not found", result.Error.Message);
        Assert.False(fixture.Tasks.Saved);
    }

    private static Fixture CreateFixture()
    {
        var memberships = new FakeMembershipReader();
        memberships.ByUser[AdminId] =
        [
            new HouseMembershipSummary(HouseId, "Casa Centro", HouseRoles.Admin),
        ];
        memberships.ByUser[MemberId] =
        [
            new HouseMembershipSummary(HouseId, "Casa Centro", HouseRoles.Member),
        ];

        var tasks = new FakeHouseTaskReader
        {
            Items =
            [
                new HouseTask(
                    TaskId,
                    HouseId,
                    "Comprar ração",
                    AdminId,
                    CreatedAt,
                    "Ração do cachorro",
                    assigneeUserId: MemberId),
            ],
            Names =
            {
                [AdminId] = "Ana Admin",
                [MemberId] = "Bruno Member",
            },
        };

        var time = new FrozenTimeProvider(CompletedAt);
        return new Fixture(
            new HouseTaskService(memberships, tasks, time),
            tasks);
    }

    private sealed record Fixture(HouseTaskService Service, FakeHouseTaskReader Tasks);

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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

    private sealed class FakeHouseTaskReader : IHouseTaskReader
    {
        public List<HouseTask> Items { get; init; } = [];

        public Dictionary<Guid, string?> Names { get; init; } = [];

        public bool Saved { get; set; }

        public Task<IReadOnlyList<HouseTaskSummary>> ListSanctuaryByHouseIdsAsync(
            IReadOnlyList<Guid> houseIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<HouseTask?> FindByIdAsync(
            Guid houseId,
            Guid taskId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(task =>
                task.Id == taskId && task.HouseId == houseId));

        public Task<HouseTaskSummary?> GetByIdAsync(
            Guid houseId,
            Guid taskId,
            CancellationToken cancellationToken)
        {
            var task = Items.SingleOrDefault(item =>
                item.Id == taskId && item.HouseId == houseId);
            return Task.FromResult(task is null ? null : ToSummary(task));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }

        private HouseTaskSummary ToSummary(HouseTask task) =>
            new(
                task.Id,
                task.HouseId,
                task.Title,
                task.Description,
                task.Status,
                task.DueAt,
                task.CompletedAt,
                task.AssigneeUserId is { } assigneeUserId
                    ? new HouseTaskMemberSummary(
                        assigneeUserId,
                        Names.GetValueOrDefault(assigneeUserId))
                    : null,
                new HouseTaskMemberSummary(
                    task.CreatedByUserId,
                    Names.GetValueOrDefault(task.CreatedByUserId)));
    }
}
