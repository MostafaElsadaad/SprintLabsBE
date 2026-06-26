using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Responses;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.CommunityGradesClasses;

internal static class CommunityGradesClassesTestHelper
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public static void SetupActiveUser(Mock<IUserService> userServiceMock, bool isPlatformAdmin = false)
    {
        userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "owner@example.com",
                Name = "Owner",
                Status = "Active",
                IsPlatformAdmin = isPlatformAdmin
            });
    }

    public static async Task SeedCommunities(
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

        await context.SaveChangesAsync();
    }

    public static async Task SeedGradesAndClasses(ApplicationDbContext context)
    {
        context.Grades.AddRange(
            new Grade
            {
                Id = 1,
                CommunityId = 1,
                Name = "Grade 5",
                SortOrder = 5
            },
            new Grade
            {
                Id = 2,
                CommunityId = 1,
                Name = "Grade 6",
                SortOrder = 6
            },
            new Grade
            {
                Id = 3,
                CommunityId = 2,
                Name = "Other Grade",
                SortOrder = 1
            });

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
                GradeId = 1,
                Name = "Deleted Class",
                Status = ClassStatus.Deleted
            },
            new ClassEntity
            {
                Id = 3,
                CommunityId = 1,
                GradeId = 2,
                Name = "Class B",
                Status = ClassStatus.Active
            },
            new ClassEntity
            {
                Id = 4,
                CommunityId = 2,
                GradeId = 3,
                Name = "Other Class",
                Status = ClassStatus.Active
            });

        await context.SaveChangesAsync();
    }
}
