using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.GetDashboardTeacher;

public class GetDashboardTeacherQuery : IRequest<TeacherView>
{
    public long UserId { get; set; }
    public long TeacherId { get; set; }
}
