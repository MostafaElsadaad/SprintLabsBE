using System.Net;

using Application.Features.Admin.Communities.AssignOwner;
using Application.Features.Admin.Communities.Common;
using Application.Features.Admin.Communities.CreateCommunity;
using Application.Features.Admin.Communities.ListCommunities;
using Application.Features.Admin.Communities.UpsertCommunityLicense;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/admin/communities")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    [Tags("Super Admin Control")]
    public class AdminCommunitiesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminCommunitiesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new CreateCommunityCommand
            {
                AuthenticatedUserId = userId.Value,
                Name = request.Name,
                AdminEmail = request.AdminEmail
            });

            return Ok(new BaseResponse<CommunityResponse>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpGet]
        public async Task<IActionResult> ListCommunities()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new ListCommunitiesQuery
            {
                AuthenticatedUserId = userId.Value
            });

            return Ok(new BaseResponse<List<CommunityListItemResponse>>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpPost("{communityId:long}/owner")]
        public async Task<IActionResult> AssignOwner(long communityId, [FromBody] AssignOwnerRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new AssignOwnerCommand
            {
                AuthenticatedUserId = userId.Value,
                CommunityId = communityId,
                Email = request.Email,
                Name = request.Name
            });

            return Ok(new BaseResponse<OwnerResponse>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpPatch("{communityId:long}/licenses")]
        public async Task<IActionResult> UpsertCommunityLicense(
            long communityId,
            [FromBody] UpsertCommunityLicenseRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new UpsertCommunityLicenseCommand
            {
                AuthenticatedUserId = userId.Value,
                CommunityId = communityId,
                MaxStudents = request.MaxStudents,
                MaxTeachers = request.MaxTeachers,
                StudentEmailChangeLimit = request.StudentEmailChangeLimit
            });

            return Ok(new BaseResponse<CommunityLicenseResponse>(
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
}
