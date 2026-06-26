using System.Net;

using Application.Features.Communities.Common;
using Application.Features.Communities.GetCommunityProfile;
using Application.Features.Communities.GradesClasses.Common;
using Application.Features.Communities.GradesClasses.CreateClass;
using Application.Features.Communities.GradesClasses.CreateGrade;
using Application.Features.Communities.GradesClasses.DeleteClass;
using Application.Features.Communities.GradesClasses.ListClasses;
using Application.Features.Communities.GradesClasses.ListGrades;
using Application.Features.Communities.GradesClasses.UpdateClass;
using Application.Features.Communities.Teachers.Common;
using Application.Features.Communities.Teachers.InviteTeacher;
using Application.Features.Communities.Teachers.ListTeachers;
using Application.Features.Communities.Teachers.RemoveTeacher;
using Application.Features.Communities.UpdateCommunityProfile;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CommunitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{communityId:long}")]
    public async Task<IActionResult> GetCommunity(long communityId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetCommunityProfileQuery
        {
            UserId = userId.Value,
            CommunityId = communityId
        });

        return Ok(new BaseResponse<CommunityProfileResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPatch("{communityId:long}")]
    public async Task<IActionResult> UpdateCommunity(
        long communityId,
        [FromBody] UpdateCommunityProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new UpdateCommunityProfileCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Name = request.Name,
            Slug = request.Slug
        });

        return Ok(new BaseResponse<CommunityProfileResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPost("{communityId:long}/teachers/invite")]
    public async Task<IActionResult> InviteTeacher(
        long communityId,
        [FromBody] InviteTeacherRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new InviteTeacherCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Email = request.Email,
            Name = request.Name
        });

        return Ok(new BaseResponse<TeacherResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/teachers")]
    public async Task<IActionResult> ListTeachers(long communityId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ListTeachersQuery
        {
            UserId = userId.Value,
            CommunityId = communityId
        });

        return Ok(new BaseResponse<List<TeacherResponse>>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpDelete("{communityId:long}/teachers/{teacherUserId:long}")]
    public async Task<IActionResult> RemoveTeacher(long communityId, long teacherUserId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RemoveTeacherCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            TeacherUserId = teacherUserId
        });

        return Ok(new BaseResponse<TeacherResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPost("{communityId:long}/grades")]
    public async Task<IActionResult> CreateGrade(
        long communityId,
        [FromBody] CreateGradeRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new CreateGradeCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Name = request.Name,
            SortOrder = request.SortOrder
        });

        return Ok(new BaseResponse<GradeResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/grades")]
    public async Task<IActionResult> ListGrades(long communityId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ListGradesQuery
        {
            UserId = userId.Value,
            CommunityId = communityId
        });

        return Ok(new BaseResponse<List<GradeResponse>>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPost("{communityId:long}/classes")]
    public async Task<IActionResult> CreateClass(
        long communityId,
        [FromBody] CreateClassRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new CreateClassCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            GradeId = request.GradeId,
            Name = request.Name
        });

        return Ok(new BaseResponse<ClassResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/classes")]
    public async Task<IActionResult> ListClasses(long communityId, [FromQuery] long? gradeId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ListClassesQuery
        {
            UserId = userId.Value,
            CommunityId = communityId,
            GradeId = gradeId
        });

        return Ok(new BaseResponse<List<ClassResponse>>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPatch("{communityId:long}/classes/{classId:long}")]
    public async Task<IActionResult> UpdateClass(
        long communityId,
        long classId,
        [FromBody] UpdateClassRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new UpdateClassCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            ClassId = classId,
            Name = request.Name,
            GradeId = request.GradeId
        });

        return Ok(new BaseResponse<ClassResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpDelete("{communityId:long}/classes/{classId:long}")]
    public async Task<IActionResult> DeleteClass(long communityId, long classId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new DeleteClassCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            ClassId = classId
        });

        return Ok(new BaseResponse<ClassResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    private long? GetUserId()
    {
        var claimValue = HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "userId")?.Value;

        return long.TryParse(claimValue, out var userId)
            ? userId
            : null;
    }
}
