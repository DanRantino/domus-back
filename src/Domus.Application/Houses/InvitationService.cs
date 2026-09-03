using Domus.Application.Common;
using Domus.Domain.Houses;

namespace Domus.Application.Houses;

public sealed class InvitationService(
    IHouseInvitationStore invitations,
    IHouseMembershipReader memberships,
    IHouseWriter houseWriter,
    IInvitationMailer mailer,
    TimeProvider time)
{
    public const int PendingCap = 20;
    public const int ExpiryDays = 7;
    public const int EmailMaxLength = 320;

    public async Task<AppResult<HouseInvitationSummary>> CreateAsync(
        Guid userId,
        string? inviterName,
        Guid houseId,
        string? email,
        string? role,
        CancellationToken cancellationToken)
    {
        var access = await RequireAdminAsync(userId, houseId, cancellationToken);
        if (!access.IsSuccess)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                access.Error!.Code,
                access.Error.Message);
        }

        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.ValidationError,
                "Email is required");
        }

        var resolvedRole = ResolveInviteRole(role);
        if (resolvedRole is null)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.ValidationError,
                "Role is invalid");
        }

        var now = time.GetUtcNow();
        var pendingCount = await invitations.CountPendingByHouseIdAsync(
            houseId,
            now,
            cancellationToken);
        if (pendingCount >= PendingCap)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.ValidationError,
                "Too many pending invitations");
        }

        var existing = await invitations.FindPendingByHouseAndEmailAsync(
            houseId,
            normalizedEmail,
            cancellationToken);
        if (existing is not null)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.Conflict,
                "Invitation already pending");
        }

        var token = InvitationTokens.Generate();
        var invitation = new HouseInvitation
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            InvitedByUserId = userId,
            Email = normalizedEmail,
            Role = resolvedRole,
            TokenHash = InvitationTokens.Hash(token),
            Status = HouseInvitationStatuses.Pending,
            ExpiresAt = now.AddDays(ExpiryDays),
            CreatedAt = now,
        };

        await invitations.AddAsync(invitation, cancellationToken);
        await invitations.SaveChangesAsync(cancellationToken);

        var emailSent = await mailer.SendAsync(
            new InvitationEmail(
                normalizedEmail,
                access.Value!.Name,
                inviterName,
                token),
            cancellationToken);

        return AppResult<HouseInvitationSummary>.Created(
            ToSummary(invitation, token, emailSent));
    }

    public async Task<AppResult<IReadOnlyList<HouseInvitationSummary>>> ListPendingAsync(
        Guid userId,
        Guid houseId,
        CancellationToken cancellationToken)
    {
        var access = await RequireAdminAsync(userId, houseId, cancellationToken);
        if (!access.IsSuccess)
        {
            return AppResult<IReadOnlyList<HouseInvitationSummary>>.Failure(
                access.Error!.Code,
                access.Error.Message);
        }

        var pending = await invitations.ListPendingByHouseIdAsync(
            houseId,
            time.GetUtcNow(),
            cancellationToken);

        return AppResult<IReadOnlyList<HouseInvitationSummary>>.Success(
            pending.Select(item => ToSummary(item)).ToArray());
    }

    public async Task<AppResult<HouseInvitationSummary>> RevokeAsync(
        Guid userId,
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var access = await RequireAdminAsync(userId, houseId, cancellationToken);
        if (!access.IsSuccess)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                access.Error!.Code,
                access.Error.Message);
        }

        var invitation = await invitations.FindByIdAsync(
            houseId,
            invitationId,
            cancellationToken);
        if (invitation is null || invitation.Status != HouseInvitationStatuses.Pending)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.NotFound,
                "Invitation not found");
        }

        invitation.Status = HouseInvitationStatuses.Revoked;
        await invitations.SaveChangesAsync(cancellationToken);

        return AppResult<HouseInvitationSummary>.Success(ToSummary(invitation));
    }

    public async Task<AppResult<HouseInvitationSummary>> ResendAsync(
        Guid userId,
        string? inviterName,
        Guid houseId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var access = await RequireAdminAsync(userId, houseId, cancellationToken);
        if (!access.IsSuccess)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                access.Error!.Code,
                access.Error.Message);
        }

        var invitation = await invitations.FindByIdAsync(
            houseId,
            invitationId,
            cancellationToken);
        if (invitation is null || invitation.Status != HouseInvitationStatuses.Pending)
        {
            return AppResult<HouseInvitationSummary>.Failure(
                ErrorCodes.NotFound,
                "Invitation not found");
        }

        var now = time.GetUtcNow();
        var token = InvitationTokens.Generate();
        invitation.TokenHash = InvitationTokens.Hash(token);
        invitation.ExpiresAt = now.AddDays(ExpiryDays);

        await invitations.SaveChangesAsync(cancellationToken);

        var emailSent = await mailer.SendAsync(
            new InvitationEmail(
                invitation.Email,
                access.Value!.Name,
                inviterName,
                token),
            cancellationToken);

        return AppResult<HouseInvitationSummary>.Success(
            ToSummary(invitation, token, emailSent));
    }

    public async Task<AppResult<InvitationPreview>> PreviewAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        var invitation = await FindUsablePendingAsync(token, cancellationToken);
        if (invitation is null)
        {
            return AppResult<InvitationPreview>.Failure(
                ErrorCodes.NotFound,
                "Invitation not found");
        }

        var houseName = invitation.House?.Name;
        if (string.IsNullOrWhiteSpace(houseName))
        {
            return AppResult<InvitationPreview>.Failure(
                ErrorCodes.NotFound,
                "Invitation not found");
        }

        return AppResult<InvitationPreview>.Success(new InvitationPreview(houseName));
    }

    public async Task<AppResult<AcceptInvitationResult>> AcceptAsync(
        Guid userId,
        string? token,
        string? callerEmail,
        CancellationToken cancellationToken)
    {
        var invitation = await FindUsablePendingAsync(token, cancellationToken);
        if (invitation is null)
        {
            return AppResult<AcceptInvitationResult>.Failure(
                ErrorCodes.NotFound,
                "Invitation not found");
        }

        var normalizedCaller = NormalizeEmail(callerEmail);
        if (normalizedCaller is null || normalizedCaller != invitation.Email)
        {
            return AppResult<AcceptInvitationResult>.Failure(
                ErrorCodes.Forbidden,
                "Forbidden");
        }

        var existing = await memberships.ListByUserIdAsync(userId, cancellationToken);
        if (existing.Any(item => item.Id == invitation.HouseId))
        {
            return AppResult<AcceptInvitationResult>.Failure(
                ErrorCodes.Conflict,
                "Already a member");
        }

        var houseName = invitation.House?.Name ?? string.Empty;
        await houseWriter.AddMemberAsync(
            userId,
            invitation.HouseId,
            invitation.Role,
            cancellationToken);

        invitation.Status = HouseInvitationStatuses.Accepted;
        invitation.AcceptedAt = time.GetUtcNow();
        invitation.AcceptedByUserId = userId;
        await invitations.SaveChangesAsync(cancellationToken);

        return AppResult<AcceptInvitationResult>.Success(
            new AcceptInvitationResult(
                invitation.HouseId,
                houseName,
                invitation.Role));
    }

    private async Task<AppResult<HouseMembershipSummary>> RequireAdminAsync(
        Guid userId,
        Guid houseId,
        CancellationToken cancellationToken)
    {
        var houses = await memberships.ListByUserIdAsync(userId, cancellationToken);
        var membership = houses.FirstOrDefault(item => item.Id == houseId);
        if (membership is null)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.NotFound,
                "House not found");
        }

        if (membership.Role != HouseRoles.Admin)
        {
            return AppResult<HouseMembershipSummary>.Failure(
                ErrorCodes.Forbidden,
                "Forbidden");
        }

        return AppResult<HouseMembershipSummary>.Success(membership);
    }

    private async Task<HouseInvitation?> FindUsablePendingAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var invitation = await invitations.FindByTokenHashAsync(
            InvitationTokens.Hash(token.Trim()),
            cancellationToken);
        if (invitation is null
            || invitation.Status != HouseInvitationStatuses.Pending
            || invitation.ExpiresAt <= time.GetUtcNow())
        {
            return null;
        }

        return invitation;
    }

    private static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > EmailMaxLength || !trimmed.Contains('@'))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    private static string? ResolveInviteRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return HouseRoles.Member;
        }

        var trimmed = role.Trim().ToLowerInvariant();
        return trimmed is HouseRoles.Admin or HouseRoles.Member
            ? trimmed
            : null;
    }

    private static HouseInvitationSummary ToSummary(
        HouseInvitation invitation,
        string? token = null,
        bool? emailSent = null) =>
        new(
            invitation.Id,
            invitation.HouseId,
            invitation.Email,
            invitation.Role,
            invitation.Status,
            invitation.ExpiresAt,
            invitation.CreatedAt,
            token,
            emailSent);
}
