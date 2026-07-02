using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Responses;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.CommunityStudentViewing;

internal static class CommunityStudentViewingTestHelper
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public static void SetupUsers(Mock<IUserService> userServiceMock)
    {
        userServiceMock
            .Setup(x => x.GetCurrentUser(It.IsAny<long>()))
            .ReturnsAsync((long userId) => new UserIdentityResponse
            {
                Id = userId,
                Email = userId == 100 ? "active.student@example.com" : $"user{userId}@example.com",
                Name = userId == 100 ? "Active Student User" : $"User {userId}",
                AvatarUrl = userId == 100 ? "https://example.com/user.png" : null,
                Status = "Active"
            });

        userServiceMock
            .Setup(x => x.GetUsersByIds(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> userIds, CancellationToken _) => userIds
                .Distinct()
                .Select(userId => new UserIdentityResponse
                {
                    Id = userId,
                    Email = userId == 100 ? "active.student@example.com" : $"user{userId}@example.com",
                    Name = userId == 100 ? "Active Student User" : $"User {userId}",
                    AvatarUrl = userId == 100 ? "https://example.com/user.png" : null,
                    Status = "Active"
                })
                .ToList());

        userServiceMock
            .Setup(x => x.SearchUserIds(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string search, CancellationToken _) =>
            {
                search = search.Trim().ToLowerInvariant();
                return "active.student@example.com".Contains(search) || "active student user".Contains(search)
                    ? new List<long> { 100 }
                    : new List<long>();
            });
    }

    public static async Task SeedRoster(
        ApplicationDbContext context,
        CommunityUserRole? role = CommunityUserRole.Owner,
        CommunityUserStatus status = CommunityUserStatus.Active)
    {
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Example School", Slug = "example-school" },
            new Community { Id = 2, Name = "Other School", Slug = "other-school" });

        if (role.HasValue)
        {
            context.CommunityUsers.Add(new CommunityUser
            {
                CommunityId = 1,
                UserId = 10,
                Role = role.Value,
                Status = status
            });
        }

        context.Grades.AddRange(
            new Grade { Id = 1, CommunityId = 1, Name = "Grade 5", SortOrder = 5 },
            new Grade { Id = 2, CommunityId = 1, Name = "Grade 6", SortOrder = 6 },
            new Grade { Id = 3, CommunityId = 2, Name = "Other Grade", SortOrder = 1 });

        context.Classes.AddRange(
            new ClassEntity
            {
                Id = 1,
                CommunityId = 1,
                GradeId = 1,
                Name = "Class A",
                Status = ClassStatus.Active
            },
            new ClassEntity
            {
                Id = 2,
                CommunityId = 1,
                GradeId = 2,
                Name = "Class B",
                Status = ClassStatus.Active
            },
            new ClassEntity
            {
                Id = 3,
                CommunityId = 2,
                GradeId = 3,
                Name = "Other Class",
                Status = ClassStatus.Active
            },
            new ClassEntity
            {
                Id = 4,
                CommunityId = 1,
                GradeId = 1,
                Name = "Deleted Class",
                Status = ClassStatus.Deleted
            });

        context.Players.AddRange(
            new Player
            {
                Id = 200,
                UserId = 100,
                GoogleId = "google-active",
                Email = "active.student@example.com",
                Name = "Active Player",
                AvatarUrl = "https://example.com/player.png",
                Gold = 7,
                Experience = 120,
                Level = 3,
                CreatedAt = DateTime.UtcNow
            },
            new Player
            {
                Id = 201,
                UserId = 101,
                GoogleId = "google-other",
                Email = "other.student@example.com",
                Name = "Other Player",
                CreatedAt = DateTime.UtcNow
            });

        context.StudentLicenses.AddRange(
            new StudentLicense
            {
                Id = 1,
                CommunityId = 1,
                Email = "pending.student@example.com",
                GradeId = 1,
                ClassId = 1,
                Status = StudentLicenseStatus.Pending,
                AssignedByUserId = 10,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new StudentLicense
            {
                Id = 2,
                CommunityId = 1,
                Email = "active.student@example.com",
                UserId = 100,
                PlayerProfileId = 200,
                GradeId = 1,
                ClassId = 1,
                Status = StudentLicenseStatus.Active,
                AssignedByUserId = 10,
                ActivatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new StudentLicense
            {
                Id = 3,
                CommunityId = 1,
                Email = "revoked.student@example.com",
                UserId = 100,
                PlayerProfileId = 200,
                GradeId = 2,
                ClassId = 2,
                Status = StudentLicenseStatus.Revoked,
                AssignedByUserId = 10,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new StudentLicense
            {
                Id = 4,
                CommunityId = 2,
                Email = "other.student@example.com",
                UserId = 101,
                PlayerProfileId = 201,
                GradeId = 3,
                ClassId = 3,
                Status = StudentLicenseStatus.Active,
                AssignedByUserId = 10,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }
}
