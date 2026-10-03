using Shared.Requests;
using Shared.Responses;
namespace Domain.Services;
public interface IMissionService
{
    Task<List<PlayerMissionResponse>> GetMyAsync(long userId, CancellationToken ct);
    Task<List<MissionClaimResponse>> ClaimAsync(long userId, long? missionId, CancellationToken ct);
    Task<List<MissionEventResponse>> EventsAsync(List<MissionEventRequest> events, CancellationToken ct);
    Task<MissionResetResponse> ResetAsync(long adminUserId, CancellationToken ct);
    Task<long> CreateTemplateAsync(long adminUserId, MissionTemplateRequest request, CancellationToken ct);
    Task<long> CreateActivationAsync(long adminUserId, MissionActivationRequest request, CancellationToken ct);
    Task<List<MissionTemplateResponse>> PreviewTemplatesAsync(long adminUserId, CancellationToken ct);
    Task<List<long>> SeedAsync(long adminUserId, CancellationToken ct);
}
