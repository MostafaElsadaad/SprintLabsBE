using API.Controllers;

using Application.Features.Accounts.CommunityAuthentication.CommunityLogin;
using Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Tests.Features.PlatformAdminPasswordAuthentication;

public class PlatformAdminAuthenticationApiContractTests
{
    [Fact]
    public void Community_login_contract_is_anonymous_and_accepts_only_identifier_and_password()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.CommunityLogin))!;

        method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().HaveCount(1);
        method.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template.Should().Be("community-login");
        typeof(CommunityLoginRequest).GetProperties().Select(x => x.Name).Should().BeEquivalentTo("Identifier", "Password");
        typeof(AccountController).GetMethod("PlatformAdminLogin").Should().BeNull();
    }

    [Fact]
    public void Set_password_route_is_not_exposed_for_platform_administrators()
    {
        typeof(AccountController).GetMethod("SetPlatformAdminPassword").Should().BeNull();
    }

    [Fact]
    public void Community_registration_is_anonymous_and_accepts_only_the_setup_fields()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.RegisterCommunity))!;

        method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().ContainSingle();
        method.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template.Should().Be("community-register");
        typeof(RegisterCommunityRequest).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo("Token", "Name", "Password");
        typeof(AccountController).GetMethod("RegisterCommunityAdmin").Should().BeNull();
    }
}
