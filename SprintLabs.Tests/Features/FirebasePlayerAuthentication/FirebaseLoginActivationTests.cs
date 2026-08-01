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

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
