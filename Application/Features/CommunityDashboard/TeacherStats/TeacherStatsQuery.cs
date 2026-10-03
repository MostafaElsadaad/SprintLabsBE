using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.TeacherStats;

public class TeacherStatsQuery : IRequest<TeacherStatsResponse>
{
    public long UserId { get; set; }
}
