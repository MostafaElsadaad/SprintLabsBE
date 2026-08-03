using API.Controllers;

using Application.Features.Accounts.TeacherAuthentication.ForgotPassword;
using Application.Features.Accounts.TeacherAuthentication.ResetPassword;
using Application.Features.Accounts.TeacherAuthentication.TeacherLogin;
using Application.Features.Communities.Teachers.InviteTeacher;
using Application.Features.CommunityInvitations.CompleteTeacherInvitation;

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
        actions.Should().Contain(new[] { "TeacherLogin", "ForgotPassword", "ResetPassword" });
    }

    [Fact]
    public void CommunityInvitationController_exposes_anonymous_validate_and_complete_actions_only()
    {
        var controllerRoute = typeof(CommunityInvitationsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>().Single();
        controllerRoute.Template.Should().Be("api/v{version:apiVersion}/community-invitations");

        var validate = typeof(CommunityInvitationsController).GetMethod("ValidateTeacherInvitation")!;
        var complete = typeof(CommunityInvitationsController).GetMethod("CompleteTeacherInvitation")!;
        validate.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().ContainSingle();
        complete.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().ContainSingle();
        typeof(CommunityInvitationsController).GetMethod("Accept").Should().BeNull();
    }

    [Fact]
    public void Public_requests_match_invitation_only_contract()
    {
        typeof(InviteTeacherRequest).GetProperty("Name").Should().BeNull();
        typeof(CompleteTeacherInvitationRequest).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Token", "Name", "Password" });
        typeof(TeacherLoginRequest).GetProperty(nameof(TeacherLoginRequest.Identifier)).Should().NotBeNull();
        typeof(ForgotPasswordRequest).GetProperty(nameof(ForgotPasswordRequest.Identifier)).Should().NotBeNull();
        typeof(ResetPasswordRequest).GetProperty(nameof(ResetPasswordRequest.UserId)).Should().NotBeNull();
    }

    [Fact]
    public void Teacher_login_response_returns_one_community()
    {
        typeof(TeacherLoginResponse).GetProperty(nameof(TeacherLoginResponse.Community)).Should().NotBeNull();
        typeof(TeacherLoginResponse).GetProperty("Communities").Should().BeNull();
        typeof(TeacherCommunityResponse).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Id", "Name" });
    }
}
