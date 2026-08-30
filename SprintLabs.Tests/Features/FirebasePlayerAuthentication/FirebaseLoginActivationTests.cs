using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseLoginActivationTests
{
    [Fact]
    public async Task ActivateEligiblePendingTeacherMemberships_ActivatesOnlyMatchingUsableInvitationWithoutChangingSeats()
    {
        await using var context = CreateContext();
        context.Communities.Add(new Community { Id = 1, Name = "Community", Slug = "community" });
        context.CommunityLicenses.Add(new CommunityLicense { CommunityId = 1, MaxTeachers = 2, UsedTeachers = 1 });
        context.CommunityUsers.Add(new CommunityUser { Id = 1, CommunityId = 1, UserId = 7, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Pending });
        context.TeacherInvitations.Add(new TeacherInvitation { CommunityUserId = 1, InvitedEmail = "teacher@example.com", TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddHours(1), CreatedByUserId = 1 });
        await context.SaveChangesAsync();
        var service = new CommunityLoginActivationService(
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<TeacherInvitation>(context));

        await service.ActivateEligiblePendingTeacherMembershipsAsync(7, "Teacher@Example.com", CancellationToken.None);

        (await context.CommunityUsers.SingleAsync()).Status.Should().Be(CommunityUserStatus.Active);
        (await context.TeacherInvitations.SingleAsync()).AcceptedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    public async Task ActivateEligiblePendingTeacherMemberships_rejects_cross_community_staff_conflicts_without_activation(
        CommunityUserRole conflictingRole,
        CommunityUserStatus conflictingStatus)
    {
        await using var context = CreateContext();
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Community 1", Slug = "community-1" },
            new Community { Id = 2, Name = "Community 2", Slug = "community-2" });
        context.CommunityUsers.AddRange(
            new CommunityUser { Id = 1, CommunityId = 1, UserId = 7, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Pending },
            new CommunityUser { Id = 2, CommunityId = 2, UserId = 7, Role = conflictingRole, Status = conflictingStatus });
        context.TeacherInvitations.Add(new TeacherInvitation
        {
            CommunityUserId = 1,
            InvitedEmail = "teacher@example.com",
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedByUserId = 1
        });
        await context.SaveChangesAsync();
        var service = new CommunityLoginActivationService(
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<TeacherInvitation>(context),
            context);

        var action = async () => await service.ActivateEligiblePendingTeacherMembershipsAsync(
            7,
            "teacher@example.com",
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<Shared.Exceptions.GenericException>();
        exception.Which.ErrorCode.Should().Be(Shared.Enums.ErrorCode.TeacherAlreadyBelongsToAnotherCommunity);
        (await context.CommunityUsers.SingleAsync(x => x.Id == 1)).Status.Should().Be(CommunityUserStatus.Pending);
        (await context.TeacherInvitations.SingleAsync()).AcceptedAt.Should().BeNull();
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
