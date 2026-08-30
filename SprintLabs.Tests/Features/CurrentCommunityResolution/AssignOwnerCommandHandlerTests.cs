using System.Net;

using Application.Features.Admin.Communities.AssignOwner;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.CurrentCommunityResolution;

public class AssignOwnerCommandHandlerTests
{
    [Theory]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    public async Task Handle_rejects_other_community_current_staff_membership_without_creating_owner(
        CommunityUserRole role,
        CommunityUserStatus status)
    {
        await using var context = CreateContext();
        await SeedAsync(context, role, status, communityId: 2);
        var handler = CreateHandler(context);

        var action = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await context.CommunityUsers.AnyAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().BeFalse();
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 2 && x.UserId == 20)).Status.Should().Be(status);
    }

    [Fact]
    public async Task Handle_reuses_removed_staff_membership_in_another_community()
    {
        await using var context = CreateContext();
        await SeedAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Removed, communityId: 2);
        var handler = CreateHandler(context);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.CommunityId.Should().Be(1);
        result.Role.Should().Be(CommunityUserRole.Owner.ToString());
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20)).Status
            .Should().Be(CommunityUserStatus.Active);
    }

    [Fact]
    public async Task Handle_converts_removed_target_membership_to_active_owner()
    {
        await using var context = CreateContext();
        await SeedAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Removed, communityId: 1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Role.Should().Be(CommunityUserRole.Owner.ToString());
        result.Status.Should().Be(CommunityUserStatus.Active.ToString());
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20)).Role
            .Should().Be(CommunityUserRole.Owner);
    }

    [Theory]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active)]
    public async Task Handle_rejects_incompatible_current_membership_in_target_community(
        CommunityUserRole role,
        CommunityUserStatus status)
    {
        await using var context = CreateContext();
        await SeedAsync(context, role, status, communityId: 1);
        var handler = CreateHandler(context);

        var action = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20)).Role.Should().Be(role);
    }

    [Fact]
    public async Task Handle_is_idempotent_for_existing_owner()
    {
        await using var context = CreateContext();
        await SeedAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, communityId: 1);
        var handler = CreateHandler(context);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        (await context.CommunityUsers.CountAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().Be(1);
    }

    private static AssignOwnerCommand ValidCommand() => new()
    {
        AuthenticatedUserId = 99,
        CommunityId = 1,
        Email = "owner@example.com",
        Name = "Owner"
    };

    private static AssignOwnerCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.GetCurrentUser(99)).ReturnsAsync(new UserIdentityResponse
        {
            Id = 99,
            IsPlatformAdmin = true,
            Status = "Active"
        });
        users.Setup(x => x.FindOrCreateBasicUser("owner@example.com", "Owner"))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 20,
                Email = "owner@example.com",
                Name = "Owner",
                Status = "Active"
            });

        return new AssignOwnerCommandHandler(
            users.Object,
            new BaseRepository<Community>(context),
            new StaffCommunityMembershipService(context));
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        CommunityUserRole role,
        CommunityUserStatus status,
        long communityId)
    {
        context.Users.Add(new User
        {
            Id = 20,
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            Name = "Owner",
            Status = UserStatus.Active
        });
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Community 1", Slug = "community-1", Status = CommunityStatus.Active },
            new Community { Id = 2, Name = "Community 2", Slug = "community-2", Status = CommunityStatus.Active });
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = communityId,
            UserId = 20,
            Role = role,
            Status = status
        });
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }
}
