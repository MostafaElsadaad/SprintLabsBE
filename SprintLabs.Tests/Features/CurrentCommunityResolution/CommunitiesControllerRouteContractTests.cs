using System.Net;
using System.Security.Claims;

using API.Controllers;

using Application.Features.Communities.Common;
using Application.Features.Communities.GetCommunityProfile;
using Application.Features.Communities.GradesClasses.Common;
using Application.Features.Communities.GradesClasses.CreateClass;
using Application.Features.Communities.GradesClasses.ListGrades;
using Application.Features.Communities.GradesClasses.UpdateClass;
using Application.Features.Communities.StudentLicenses.AddStudentLicense;
using Application.Features.Communities.StudentLicenses.UpdateStudentLicense;
using Application.Features.Communities.Teachers.InviteTeacher;
using Application.Features.Communities.Teachers.ReplaceTeacherClassAssignments;
using Application.Features.Communities.UpdateCommunityProfile;

using Domain.Services;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

using Moq;

using Shared.Exceptions;

namespace Compass.Tests.Features.CurrentCommunityResolution;

public class CommunitiesControllerRouteContractTests
{
    public static TheoryData<string, Type, string> StaffRoutes => new()
    {
        { nameof(CommunitiesController.GetCurrentCommunity), typeof(HttpGetAttribute), "me" },
        { nameof(CommunitiesController.UpdateCurrentCommunity), typeof(HttpPatchAttribute), "me" },
        { nameof(CommunitiesController.ListTeachers), typeof(HttpGetAttribute), "teachers" },
        { nameof(CommunitiesController.InviteTeacher), typeof(HttpPostAttribute), "teachers/invite" },
        { nameof(CommunitiesController.RemoveTeacher), typeof(HttpDeleteAttribute), "teachers/{teacherUserId:long}" },
        { nameof(CommunitiesController.ReplaceTeacherClassAssignments), typeof(HttpPutAttribute), "teachers/{teacherUserId:long}/classes" },
        { nameof(CommunitiesController.ListGrades), typeof(HttpGetAttribute), "grades" },
        { nameof(CommunitiesController.CreateClass), typeof(HttpPostAttribute), "classes" },
        { nameof(CommunitiesController.ListClasses), typeof(HttpGetAttribute), "classes" },
        { nameof(CommunitiesController.UpdateClass), typeof(HttpPatchAttribute), "classes/{classId:long}" },
        { nameof(CommunitiesController.DeleteClass), typeof(HttpDeleteAttribute), "classes/{classId:long}" },
        { nameof(CommunitiesController.AddStudentLicense), typeof(HttpPostAttribute), "student-licenses" },
        { nameof(CommunitiesController.ListStudentLicenses), typeof(HttpGetAttribute), "student-licenses" },
        { nameof(CommunitiesController.UpdateStudentLicense), typeof(HttpPatchAttribute), "student-licenses/{licenseId:long}" },
        { nameof(CommunitiesController.RevokeStudentLicense), typeof(HttpDeleteAttribute), "student-licenses/{licenseId:long}" },
        { nameof(CommunitiesController.ListStudents), typeof(HttpGetAttribute), "students" },
        { nameof(CommunitiesController.GetStudentDetail), typeof(HttpGetAttribute), "students/{playerProfileId:long}" }
    };

    [Theory]
    [MemberData(nameof(StaffRoutes))]
    public void Staff_routes_are_tenantless(string methodName, Type attributeType, string expectedTemplate)
    {
        var method = typeof(CommunitiesController).GetMethod(methodName)!;

        method.Should().NotBeNull();
        var attribute = method.GetCustomAttributes(attributeType, true).Cast<HttpMethodAttribute>().Single();
        attribute.Template.Should().Be(expectedTemplate);
        method.GetParameters().Should().NotContain(x =>
            string.Equals(x.Name, "communityId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Staff_request_bodies_do_not_accept_community_id()
    {
        var requestTypes = new[]
        {
            typeof(UpdateCommunityProfileRequest),
            typeof(InviteTeacherRequest),
            typeof(ReplaceTeacherClassAssignmentsRequest),
            typeof(CreateClassRequest),
            typeof(UpdateClassRequest),
            typeof(AddStudentLicenseRequest),
            typeof(UpdateStudentLicenseRequest)
        };

        requestTypes.Should().AllSatisfy(type =>
            type.GetProperty("CommunityId").Should().BeNull($"{type.Name} is supplied by the staff client"));
    }

    [Fact]
    public async Task Staff_action_dispatches_the_database_resolved_community_id()
    {
        var mediator = new Mock<IMediator>();
        ListGradesQuery? dispatched = null;
        mediator.Setup(x => x.Send(It.IsAny<IRequest<List<GradeResponse>>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<List<GradeResponse>>, CancellationToken>((request, _) => dispatched = request as ListGradesQuery)
            .ReturnsAsync(new List<GradeResponse>());
        var access = new Mock<ICommunityAccessService>();
        access.Setup(x => x.ResolveCurrentStaffCommunityId(42, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var controller = CreateController(mediator.Object, access.Object, "42");

        var result = await controller.ListGrades();

        result.Should().BeOfType<OkObjectResult>();
        dispatched.Should().NotBeNull();
        dispatched!.UserId.Should().Be(42);
        dispatched.CommunityId.Should().Be(7);
    }

    [Fact]
    public async Task Resolver_failure_is_forbidden_before_mediatr_dispatch()
    {
        var mediator = new Mock<IMediator>();
        var access = new Mock<ICommunityAccessService>();
        access.Setup(x => x.ResolveCurrentStaffCommunityId(42, It.IsAny<CancellationToken>())).ReturnsAsync((long?)null);
        var controller = CreateController(mediator.Object, access.Object, "42");

        var action = async () => await controller.ListGrades();

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Malformed_identity_is_unauthorized_before_resolution_or_dispatch()
    {
        var mediator = new Mock<IMediator>();
        var access = new Mock<ICommunityAccessService>();
        var controller = CreateController(mediator.Object, access.Object, "not-a-user-id");

        var result = await controller.ListGrades();

        result.Should().BeOfType<UnauthorizedResult>();
        access.VerifyNoOtherCalls();
        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public void Student_profile_and_platform_admin_routes_keep_explicit_community_id()
    {
        RouteTemplate<CommunitiesController>(nameof(CommunitiesController.GetCommunity), typeof(HttpGetAttribute))
            .Should().Be("{communityId:long}");
        RouteTemplate<AdminCommunitiesController>(nameof(AdminCommunitiesController.AssignOwner), typeof(HttpPostAttribute))
            .Should().Be("{communityId:long}/owner");
        RouteTemplate<AdminCommunitiesController>(nameof(AdminCommunitiesController.UpsertCommunityLicense), typeof(HttpPatchAttribute))
            .Should().Be("{communityId:long}/licenses");
    }

    [Fact]
    public void No_staff_action_retains_a_numeric_community_route_template()
    {
        var allowed = nameof(CommunitiesController.GetCommunity);
        var actionTemplates = typeof(CommunitiesController)
            .GetMethods()
            .Where(x => x.Name != allowed)
            .SelectMany(x => x.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>())
            .Select(x => x.Template)
            .Where(x => x != null)
            .ToList();

        actionTemplates.Should().NotContain(template => template!.Contains("{communityId:long}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Staff_grade_creation_route_is_not_exposed()
    {
        typeof(CommunitiesController).GetMethod("CreateGrade").Should().BeNull();
    }

    [Fact]
    public async Task Retained_numeric_profile_route_dispatches_the_existing_profile_query_for_student_compatibility()
    {
        var mediator = new Mock<IMediator>();
        GetCommunityProfileQuery? dispatched = null;
        mediator.Setup(x => x.Send(It.IsAny<IRequest<CommunityProfileResponse>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CommunityProfileResponse>, CancellationToken>((request, _) => dispatched = request as GetCommunityProfileQuery)
            .ReturnsAsync(new CommunityProfileResponse());
        var controller = CreateController(mediator.Object, new Mock<ICommunityAccessService>().Object, "77");

        var result = await controller.GetCommunity(8);

        result.Should().BeOfType<OkObjectResult>();
        dispatched.Should().NotBeNull();
        dispatched!.UserId.Should().Be(77);
        dispatched.CommunityId.Should().Be(8);
    }

    private static CommunitiesController CreateController(
        IMediator mediator,
        ICommunityAccessService access,
        string userId)
    {
        return new CommunitiesController(mediator, access)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", userId) }, "test"))
                }
            }
        };
    }

    private static string? RouteTemplate<TController>(string methodName, Type attributeType)
    {
        return typeof(TController).GetMethod(methodName)!
            .GetCustomAttributes(attributeType, true)
            .Cast<HttpMethodAttribute>()
            .Single()
            .Template;
    }
}
