using API.Controllers;

using System.Reflection;

using Application.Features.Accounts.TeacherAuthentication.ForgotPassword;
using Application.Features.Accounts.TeacherAuthentication.ResetPassword;
using Application.Features.Accounts.CommunityAuthentication.CommunityLogin;
using Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;
using Application.Features.Admin.Communities.CreateCommunity;
using Application.Features.Communities.Teachers.InviteTeacher;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Responses;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class InvitationApiContractTests
{
    [Fact]
    public void AccountController_does_not_expose_public_teacher_registration_or_confirmation_actions()
    {
        var actions = typeof(AccountController).GetMethods().Select(x => x.Name).ToList();

        actions.Should().NotContain(new[] { "RegisterTeacher", "ConfirmEmail", "ResendConfirmation" });
        actions.Should().Contain(new[] { "CommunityLogin", "ForgotPassword", "ResetPassword" });
        actions.Should().NotContain("TeacherLogin");
    }

    [Fact]
    public void CommunityInvitationController_keeps_only_teacher_invitation_validation()
    {
        var controllerRoute = typeof(CommunityInvitationsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>().Single();
        controllerRoute.Template.Should().Be("api/v{version:apiVersion}/community-invitations");

        var validate = typeof(CommunityInvitationsController).GetMethod("ValidateTeacherInvitation")!;
        validate.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().ContainSingle();
        typeof(CommunityInvitationsController).GetMethod("CompleteTeacherInvitation").Should().BeNull();
        typeof(CommunityInvitationsController).GetMethod("ValidateCommunityInvitation").Should().BeNull();
        typeof(CommunityInvitationsController).GetMethod("CompleteCommunityInvitation").Should().BeNull();
        typeof(CommunityInvitationsController).GetMethod("Accept").Should().BeNull();
    }

    [Fact]
    public void Public_requests_match_invitation_only_contract()
    {
        typeof(InviteTeacherRequest).GetProperty("Name").Should().BeNull();
        typeof(RegisterCommunityRequest).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Token", "Name", "Password" });
        typeof(CommunityLoginRequest).GetProperty(nameof(CommunityLoginRequest.Identifier)).Should().NotBeNull();
        typeof(ForgotPasswordRequest).GetProperty(nameof(ForgotPasswordRequest.Identifier)).Should().NotBeNull();
        typeof(ResetPasswordRequest).GetProperty(nameof(ResetPasswordRequest.UserId)).Should().NotBeNull();
        typeof(CreateCommunityRequest).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Name", "AdminEmail" });
    }

    [Fact]
    public void Teacher_invitation_uses_the_authenticated_owners_resolved_community()
    {
        var action = typeof(CommunitiesController).GetMethod(nameof(CommunitiesController.InviteTeacher))!;

        action.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template
            .Should().Be("teachers/invite");
        action.GetParameters().Select(x => x.Name).Should().BeEquivalentTo("request");
        typeof(InviteTeacherRequest).GetProperty("CommunityId").Should().BeNull();
        typeof(InviteTeacherCommand).GetProperty("CommunityId").Should().NotBeNull();
    }

    [Fact]
    public void Admin_community_apis_are_grouped_as_super_admin_control_in_swagger()
    {
        var tags = typeof(AdminCommunitiesController).GetCustomAttributesData()
            .Single(x => x.AttributeType.Name == "TagsAttribute")
            .ConstructorArguments
            .Single()
            .Value
            .Should()
            .BeAssignableTo<IEnumerable<CustomAttributeTypedArgument>>()
            .Which;

        tags.Select(x => x.Value).Should().Contain("Super Admin Control");
    }

    [Fact]
    public void Community_login_response_returns_one_community()
    {
        typeof(CommunityLoginResponse).GetProperty(nameof(CommunityLoginResponse.Community)).Should().NotBeNull();
        typeof(CommunityLoginResponse).GetProperty("Communities").Should().BeNull();
        typeof(TeacherCommunityResponse).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Id", "Name" });
    }
}
