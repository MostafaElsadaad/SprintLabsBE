using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.UpdateDashboardTeacher;

public class UpdateDashboardTeacherCommand : IRequest<TeacherView>
{
    public long UserId { get; set; }
    public long TeacherId { get; set; }
    public UpdateDashboardTeacherRequest Body { get; set; } = new();
}
