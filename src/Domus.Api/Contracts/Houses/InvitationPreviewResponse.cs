using Domus.Application.Houses;

namespace Domus.Api.Contracts.Houses;

public sealed record InvitationPreviewResponse(string HouseName)
{
    public static InvitationPreviewResponse FromApplication(InvitationPreview preview) =>
        new(preview.HouseName);
}
