using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.AssignDashboardTeacher;

public class AssignDashboardTeacherCommand : IRequest<bool>
{
    public long UserId { get; set; }
    public long ClassId { get; set; }
    public long TeacherId { get; set; }
}
