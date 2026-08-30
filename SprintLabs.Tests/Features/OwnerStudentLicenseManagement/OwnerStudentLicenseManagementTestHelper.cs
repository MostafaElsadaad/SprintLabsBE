using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Responses;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.OwnerStudentLicenseManagement;

internal static class OwnerStudentLicenseManagementTestHelper
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public static void SetupUsers(Mock<IUserService> userServiceMock, bool ownerSuspended = false)
    {
        userServiceMock
            .Setup(x => x.GetCurrentUser(It.IsAny<long>()))
            .ReturnsAsync((long userId) => new UserIdentityResponse
            {
                Id = userId,
                Email = $"user{userId}@example.com",
                Name = $"User {userId}",
                Status = ownerSuspended && userId == 10 ? "Suspended" : "Active",
                IsSuspended = ownerSuspended && userId == 10
            });
    }

    public static async Task SeedBase(
        ApplicationDbContext context,
        CommunityUserRole ownerRole = CommunityUserRole.Owner,
        CommunityUserStatus ownerStatus = CommunityUserStatus.Active,
        int maxStudents = 5,
        int usedStudents = 0,
        int emailChangeLimit = 2)
    {
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Example School", Slug = "example-school" },
            new Community { Id = 2, Name = "Other School", Slug = "other-school" });

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
            MaxStudents = maxStudents,
            UsedStudents = usedStudents,
            StudentEmailChangeLimit = emailChangeLimit
        });

        context.Grades.AddRange(
            new Grade { Id = 1, CommunityId = 1, Value = 7, Name = "Grade 7", SortOrder = 7 },
            new Grade { Id = 2, CommunityId = 1, Value = 8, Name = "Grade 8", SortOrder = 8 },
            new Grade { Id = 3, CommunityId = 2, Value = 7, Name = "Grade 7", SortOrder = 7 });

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

        await context.SaveChangesAsync();
    }
}
