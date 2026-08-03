using System.Net;

using Application.Features.Communities.Teachers.InviteTeacher;

using Domain.Services;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;
using Shared.Responses;

namespace Compass.Tests.Features.OwnerTeacherManagement;

public class InviteTeacherCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveOwner_DelegatesInvitationToSingleServiceOwner()
    {
        var access = new Mock<ICommunityAccessService>();
        access.Setup(x => x.HasCommunityRole(10, 1, It.IsAny<IEnumerable<Domain.Enums.CommunityUserRole>>())).ReturnsAsync(true);
        var invitations = new Mock<ITeacherInvitationService>();
        invitations.Setup(x => x.IssueAsync(10, 1, "teacher@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInvitationIssueResult { UserId = 20, Name = "Teacher", Email = "teacher@example.com", CommunityId = 1, CommunityName = "Alpha", Status = "Pending", InvitationToken = "token" });
        var email = new Mock<IEmailService>();
        var handler = new InviteTeacherCommandHandler(access.Object, invitations.Object, email.Object, Options.Create(new FrontendOptions { BaseUrl = "https://app.example" }));

        var result = await handler.Handle(new InviteTeacherCommand { UserId = 10, CommunityId = 1, Email = " Teacher@Example.com " }, CancellationToken.None);

        result.Status.Should().Be("Pending");
        invitations.Verify(x => x.IssueAsync(10, 1, "teacher@example.com", It.IsAny<CancellationToken>()), Times.Once);
        email.Verify(x => x.SendCommunityInvitationEmailAsync("teacher@example.com", "Teacher", "Alpha", It.Is<string>(url => url.Contains("/invitations/teacher/setup?token=token")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonOwner_RejectsBeforeInvitationService()
    {
        var access = new Mock<ICommunityAccessService>();
        access.Setup(x => x.HasCommunityRole(10, 1, It.IsAny<IEnumerable<Domain.Enums.CommunityUserRole>>())).ReturnsAsync(false);
        var invitations = new Mock<ITeacherInvitationService>();
        var handler = new InviteTeacherCommandHandler(access.Object, invitations.Object, Mock.Of<IEmailService>(), Options.Create(new FrontendOptions { BaseUrl = "https://app.example" }));

        var action = async () => await handler.Handle(new InviteTeacherCommand { UserId = 10, CommunityId = 1, Email = "teacher@example.com" }, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        invitations.VerifyNoOtherCalls();
    }

    [Fact]
    public void Handler_HasExactlyOnePublicConstructor()
    {
        typeof(InviteTeacherCommandHandler).GetConstructors().Should().ContainSingle();
    }
}