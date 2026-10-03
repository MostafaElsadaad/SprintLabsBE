using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardGrades;

public class ListDashboardGradesQuery : IRequest<List<GradeView>>
{
    public long UserId { get; set; }
}
