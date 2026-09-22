using System.Security.Claims;
using Domus.Api.Http;
using Domus.Application.Users;
using Domus.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace Domus.Api.Tests;

public sealed class CurrentUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Anonymous_DoesNotSetCurrentUser()
    {
        var store = new FakeUserStore();
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, store);

        Assert.True(nextCalled);
        Assert.False(CurrentUserContext.TryGet(context, out _));
        Assert.Equal(0, store.FindByIdentityIdCalls);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedWithoutSub_DoesNotSetCurrentUser()
    {
        var store = new FakeUserStore();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("name", "Ada")], "Test")),
        };
        var nextCalled = false;

        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, store);

        Assert.True(nextCalled);
        Assert.False(CurrentUserContext.TryGet(context, out _));
        Assert.Equal(0, store.FindByIdentityIdCalls);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUnprovisioned_DoesNotSetCurrentUser()
    {
        var store = new FakeUserStore();
        var context = AuthenticatedContext("identity-missing");
        var nextCalled = false;

        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, store);

        Assert.True(nextCalled);
        Assert.False(CurrentUserContext.TryGet(context, out _));
        Assert.Equal(1, store.FindByIdentityIdCalls);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedProvisioned_SetsCurrentUser()
    {
        var user = new User(Guid.NewGuid(), "identity-provisioned", "Ada Lovelace");
        var store = new FakeUserStore();
        store.Add(user);
        var context = AuthenticatedContext(user.IdentityId);
        var nextCalled = false;

        var middleware = new CurrentUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, store);

        Assert.True(nextCalled);
        Assert.True(CurrentUserContext.TryGet(context, out var currentUser));
        Assert.Equal(user.Id, currentUser.Id);
        Assert.Equal(user.IdentityId, currentUser.IdentityId);
        Assert.Equal(user.FullName, currentUser.FullName);
        Assert.Equal(user.NotifyDailyTasks, currentUser.NotifyDailyTasks);
        Assert.Equal(user.NotifyExpenses, currentUser.NotifyExpenses);
        Assert.Equal(user.NotifyFamilyChat, currentUser.NotifyFamilyChat);
        Assert.Equal(user.Theme, currentUser.Theme);
    }

    private static DefaultHttpContext AuthenticatedContext(string identityId)
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", identityId)], "Test")),
        };
    }

    private sealed class FakeUserStore : IUserStore
    {
        private readonly Dictionary<string, User> _users = new(StringComparer.Ordinal);

        public int FindByIdentityIdCalls { get; private set; }

        public void Add(User user) => _users[user.IdentityId] = user;

        public Task<User?> FindByIdentityIdAsync(
            string identityId,
            CancellationToken cancellationToken)
        {
            FindByIdentityIdCalls++;
            _users.TryGetValue(identityId, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> FindTrackedByIdentityIdAsync(
            string identityId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<User> AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> SaveChangesIgnoringUniqueViolationAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
