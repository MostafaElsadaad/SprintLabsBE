using System.Net;

using Application.Features.Communities.Teachers.InviteTeacher;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.OwnerTeacherManagement;

public class InviteTeacherCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ActiveOwnerWithCapacity_CreatesPendingTeacherAndIncrementsUsedTeachers()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 0, maxTeachers: 2);
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "  Teacher@Example.com ",
            Name = " Teacher Name "
        }, CancellationToken.None);

        result.UserId.Should().Be(20);
        result.Email.Should().Be("teacher@example.com");
        result.Status.Should().Be(CommunityUserStatus.Pending.ToString());

        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Role.Should().Be(CommunityUserRole.Teacher);
        membership.Status.Should().Be(CommunityUserStatus.Pending);

        var license = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        license.UsedTeachers.Should().Be(1);

        _userServiceMock.Verify(x => x.FindOrCreateBasicUser("teacher@example.com", "Teacher Name"), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRegisteredTeacherWithCapacity_CreatesActiveTeacherAndIncrementsUsedTeachers()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 0, maxTeachers: 2);
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20, hasGoogleIdentity: true);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        result.Status.Should().Be(CommunityUserStatus.Active.ToString());

        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Role.Should().Be(CommunityUserRole.Teacher);
        membership.Status.Should().Be(CommunityUserStatus.Active);

        var license = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        license.UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExistingPendingTeacher_ReturnsMembershipWithoutIncrementingUsedTeachers()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 1, maxTeachers: 2);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        result.Status.Should().Be(CommunityUserStatus.Pending.ToString());
        (await context.CommunityUsers.CountAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().Be(1);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExistingPendingRegisteredTeacher_ActivatesWithoutIncrementingUsedTeachersAgain()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 1, maxTeachers: 2);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20, hasGoogleIdentity: true);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        result.Status.Should().Be(CommunityUserStatus.Active.ToString());

        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Status.Should().Be(CommunityUserStatus.Active);
        membership.UpdatedAt.Should().NotBeNull();

        (await context.CommunityUsers.CountAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().Be(1);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RemovedTeacherWithCapacity_RestoresToPendingAndIncrementsUsedTeachers()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 0, maxTeachers: 2);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Removed,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await context.SaveChangesAsync();
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20);
        var handler = CreateHandler(context);

        await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Status.Should().Be(CommunityUserStatus.Pending);
        membership.UpdatedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RemovedRegisteredTeacherWithCapacity_RestoresToActiveAndIncrementsUsedTeachers()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 0, maxTeachers: 2);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Removed,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await context.SaveChangesAsync();
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20, hasGoogleIdentity: true);
        var handler = CreateHandler(context);

        await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Status.Should().Be(CommunityUserStatus.Active);
        membership.UpdatedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FullTeacherLicense_RejectsInviteWithoutCreatingMembership()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(context, usedTeachers: 2, maxTeachers: 2);
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
        (await context.CommunityUsers.AnyAsync(x => x.UserId == 20)).Should().BeFalse();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UserWithoutActiveOwnerRole_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedOwnerAndLicense(
            context,
            usedTeachers: 0,
            maxTeachers: 2,
            ownerRole: CommunityUserRole.Teacher);
        SetupActiveOwner();
        SetupTeacher("teacher@example.com", 20);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new InviteTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "teacher@example.com",
            Name = "Teacher Name"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private InviteTeacherCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new InviteTeacherCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<CommunityLicense>(context));
    }

    private void SetupActiveOwner()
    {
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "owner@example.com",
                Name = "Owner",
                Status = "Active"
            });
    }

    private void SetupTeacher(string email, long userId, bool hasGoogleIdentity = false)
    {
        _userServiceMock
            .Setup(x => x.FindOrCreateBasicUser(email, "Teacher Name"))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = userId,
                Email = email,
                Name = "Teacher Name",
                Status = "Active",
                HasGoogleIdentity = hasGoogleIdentity
            });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedOwnerAndLicense(
        ApplicationDbContext context,
        int usedTeachers,
        int maxTeachers,
        CommunityUserRole ownerRole = CommunityUserRole.Owner,
        CommunityUserStatus ownerStatus = CommunityUserStatus.Active)
    {
        context.Communities.Add(new Community
        {
            Id = 1,
            Name = "Example School",
            Slug = "example-school",
            Status = CommunityStatus.Active
        });

        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 10,
            Role = ownerRole,
            Status = ownerStatus
        });

        context.CommunityLicenses.Add(new CommunityLicense
        {
            CommunityId = 1,
            MaxTeachers = maxTeachers,
            UsedTeachers = usedTeachers
        });

        await context.SaveChangesAsync();
    }
}
