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
using Application.Features.Communities.StudentLicenses.AddStudentLicense;
using Application.Features.Communities.StudentLicenses.Common;
using Application.Features.Communities.StudentLicenses.ListStudentLicenses;
using Application.Features.Communities.StudentLicenses.RevokeStudentLicense;
using Application.Features.Communities.StudentLicenses.UpdateStudentLicense;
using Application.Features.Communities.Students.Common;
using Application.Features.Communities.Students.GetStudentDetail;
using Application.Features.Communities.Students.ListStudents;
using Application.Features.Communities.Teachers.Common;
using Application.Features.Communities.Teachers.InviteTeacher;
using Application.Features.Communities.Teachers.ListTeachers;
using Application.Features.Communities.Teachers.RemoveTeacher;
using Application.Features.Communities.UpdateCommunityProfile;

using Domain.Enums;

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
            Email = request.Email
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

    [HttpPost("{communityId:long}/student-licenses")]
    public async Task<IActionResult> AddStudentLicense(
        long communityId,
        [FromBody] AddStudentLicenseRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new AddStudentLicenseCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Email = request.Email,
            GradeId = request.GradeId,
            ClassId = request.ClassId
        });

        return Ok(new BaseResponse<StudentLicenseResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/student-licenses")]
    public async Task<IActionResult> ListStudentLicenses(
        long communityId,
        [FromQuery] StudentLicenseStatus? status,
        [FromQuery] long? gradeId,
        [FromQuery] long? classId,
        [FromQuery] string? search)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ListStudentLicensesQuery
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Status = status,
            GradeId = gradeId,
            ClassId = classId,
            Search = search
        });

        return Ok(new BaseResponse<List<StudentLicenseResponse>>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/students")]
    public async Task<IActionResult> ListStudents(
        long communityId,
        [FromQuery] StudentLicenseStatus? status,
        [FromQuery] long? gradeId,
        [FromQuery] long? classId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ListStudentsQuery
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Status = status,
            GradeId = gradeId,
            ClassId = classId,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return Ok(new BaseResponse<PagedResponse<CommunityStudentListItemResponse>>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpGet("{communityId:long}/students/{playerProfileId:long}")]
    public async Task<IActionResult> GetStudentDetail(long communityId, long playerProfileId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetStudentDetailQuery
        {
            UserId = userId.Value,
            CommunityId = communityId,
            PlayerProfileId = playerProfileId
        });

        return Ok(new BaseResponse<CommunityStudentDetailResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPatch("{communityId:long}/student-licenses/{licenseId:long}")]
    public async Task<IActionResult> UpdateStudentLicense(
        long communityId,
        long licenseId,
        [FromBody] UpdateStudentLicenseRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new UpdateStudentLicenseCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            LicenseId = licenseId,
            Email = request.Email,
            GradeId = request.GradeId,
            ClassId = request.ClassId
        });

        return Ok(new BaseResponse<StudentLicenseResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpDelete("{communityId:long}/student-licenses/{licenseId:long}")]
    public async Task<IActionResult> RevokeStudentLicense(long communityId, long licenseId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RevokeStudentLicenseCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            LicenseId = licenseId
        });

        return Ok(new BaseResponse<StudentLicenseResponse>(
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