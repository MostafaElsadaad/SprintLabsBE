namespace Application.Features.CommunityDashboard.Common;

public record DashboardScope(long UserId, long CommunityId, bool IsOwner, List<long> ClassIds);
