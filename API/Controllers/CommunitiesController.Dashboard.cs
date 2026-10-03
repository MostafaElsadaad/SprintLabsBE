using System.Net;
using Application.Features.CommunityDashboard.Common;
using Application.Features.CommunityDashboard.GetDashboardClass;
using Application.Features.CommunityDashboard.GetDashboardTeacher;
using Application.Features.CommunityDashboard.AssignDashboardTeacher;
using Application.Features.CommunityDashboard.UpdateDashboardTeacher;
using Application.Features.CommunityDashboard.MessageDashboardTeacher;
using Application.Features.CommunityDashboard.TeacherStats;
using Application.Features.CommunityDashboard.TeacherActivity;
using Application.Features.CommunityDashboard.ListDashboardInvitations;
using Application.Features.CommunityDashboard.ResendDashboardInvitation;
using Application.Features.CommunityDashboard.CancelDashboardInvitation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace API.Controllers;

public partial class CommunitiesController
{
    [HttpGet("classes/{classId:long}")]
    public Task<IActionResult> GetClass(long classId) => SendDashboard(
        new GetDashboardClassQuery { UserId = DashboardCallerId(), ClassId = classId }, DashboardResponseMapper.ClassDetail);

    [HttpPost("classes/{classId:long}/assign-teacher")]
    public Task<IActionResult> AssignTeacher(long classId, [FromBody] AssignDashboardTeacherRequest body) => SendDashboard(
        new AssignDashboardTeacherCommand { UserId = DashboardCallerId(), ClassId = classId, TeacherId = body.TeacherId });

    [HttpGet("teachers/{teacherId:long}")]
    public Task<IActionResult> GetTeacher(long teacherId) => SendDashboard(
        new GetDashboardTeacherQuery { UserId = DashboardCallerId(), TeacherId = teacherId }, DashboardResponseMapper.TeacherDetail);

    [HttpGet("teachers/stats")]
    public Task<IActionResult> GetTeacherStats() => SendDashboard(new TeacherStatsQuery { UserId = DashboardCallerId() });

    [HttpGet("teachers/activity")]
    public Task<IActionResult> GetTeacherActivity([FromQuery] TeacherActivityQuery query, [FromQuery] int? page = null)
    {
        query.UserId = DashboardCallerId();
        if (page.HasValue) query.PageNumber = page.Value;
        return SendDashboard(query, x => DashboardResponseMapper.Page(x, DashboardResponseMapper.TeacherActivity));
    }

    [HttpPatch("teachers/{teacherId:long}")]
    public Task<IActionResult> UpdateTeacher(long teacherId, [FromBody] UpdateDashboardTeacherRequest body) => SendDashboard(
        new UpdateDashboardTeacherCommand { UserId = DashboardCallerId(), TeacherId = teacherId, Body = body }, DashboardResponseMapper.TeacherDetail);

    [HttpPost("teachers/{teacherId:long}/message")]
    public Task<IActionResult> MessageTeacher(long teacherId, [FromBody] MessageDashboardTeacherRequest body) => SendDashboard(
        new MessageDashboardTeacherCommand { UserId = DashboardCallerId(), TeacherId = teacherId, Body = body });

    [HttpGet("invitations")]
    public Task<IActionResult> GetInvitations() => SendDashboard(new ListDashboardInvitationsQuery { UserId = DashboardCallerId() });

    [HttpPost("invitations/{invitationId:long}/resend")]
    public Task<IActionResult> ResendInvitation(long invitationId) => SendDashboard(
        new ResendDashboardInvitationCommand { UserId = DashboardCallerId(), InvitationId = invitationId });

    [HttpDelete("invitations/{invitationId:long}")]
    public Task<IActionResult> CancelInvitation(long invitationId) => SendDashboard(
        new CancelDashboardInvitationCommand { UserId = DashboardCallerId(), InvitationId = invitationId });

    private async Task<IActionResult> SendDashboard<T>(IRequest<T> request, Func<T, object>? map = null)
    {
        var result = await _mediator.Send(request, HttpContext.RequestAborted);
        return Ok(new BaseResponse<object>(map == null ? result! : map(result), ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    private long DashboardCallerId()
    {
        var id = GetUserId();
        if (!id.HasValue || id.Value <= 0)
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        return id.Value;
    }
}
