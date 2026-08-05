using API.Controllers;

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
    public void Community_login_response_returns_one_community()
    {
        typeof(CommunityLoginResponse).GetProperty(nameof(CommunityLoginResponse.Community)).Should().NotBeNull();
        typeof(CommunityLoginResponse).GetProperty("Communities").Should().BeNull();
        typeof(TeacherCommunityResponse).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "Id", "Name" });
    }
}
