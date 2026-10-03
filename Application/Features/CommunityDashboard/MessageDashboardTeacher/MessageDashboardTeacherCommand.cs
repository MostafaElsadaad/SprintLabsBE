using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.MessageDashboardTeacher;

public class MessageDashboardTeacherCommand : IRequest<long>
{
    public long UserId { get; set; }
    public long TeacherId { get; set; }
    public MessageDashboardTeacherRequest Body { get; set; } = new();
}
