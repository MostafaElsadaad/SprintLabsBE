using Application.Features.CommunityDashboard.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Responses;

namespace Compass.Tests.Features.CommunityDashboard;

internal sealed class DashboardFixture : IDisposable
{
    public ApplicationDbContext Context { get; }
    public Mock<IUserService> Users { get; } = new();
    public DashboardAuthorization Authorization { get; }
    public DashboardProjection Projection { get; }

    public DashboardFixture()
    {
        Context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Users.Setup(x => x.GetCurrentUser(It.IsAny<long>())).ReturnsAsync((long id) =>
            Context.Users.Where(x => x.Id == id).Select(x => new UserIdentityResponse
            { Id = x.Id, Name = x.Name, Email = x.Email!, IsSuspended = x.Status == UserStatus.Suspended }).SingleOrDefault());
        Users.Setup(x => x.GetUsersByIds(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, CancellationToken ct) =>
                Context.Users.Where(x => ids.Contains(x.Id)).Select(x => new UserIdentityResponse
                { Id = x.Id, Name = x.Name, Email = x.Email! }).ToList());
        Authorization = new DashboardAuthorization(Users.Object, new CommunityAccessService(Repo<CommunityUser>()), Repo<TeacherClassAssignment>(), Repo<Class>());
        Projection = new DashboardProjection(Repo<Class>(), Repo<StudentLicense>(), Repo<Player>(),
            Repo<CommunityUser>(), Repo<TeacherClassAssignment>(), Repo<StaffActivity>(), Users.Object);
    }

    public BaseRepository<T> Repo<T>() where T : class => new(Context);

    public async Task SeedAsync()
    {
        Context.Communities.AddRange(new Community { Id = 1, Name = "School One", Slug = "one" },
            new Community { Id = 2, Name = "School Two", Slug = "two" });
        foreach (var id in new long[] { 10, 20, 21, 30, 40 })
            Context.Users.Add(new User { Id = id, Name = $"User {id}", Email = $"user{id}@example.com",
                UserName = $"user{id}@example.com", NormalizedEmail = $"USER{id}@EXAMPLE.COM",
                NormalizedUserName = $"USER{id}@EXAMPLE.COM", EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString() });
        Context.CommunityUsers.AddRange(
            new CommunityUser { CommunityId = 1, UserId = 10, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active },
            new CommunityUser { CommunityId = 1, UserId = 20, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Active },
            new CommunityUser { CommunityId = 1, UserId = 21, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Active },
            new CommunityUser { CommunityId = 2, UserId = 30, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        Context.CommunityLicenses.AddRange(new CommunityLicense { CommunityId = 1, MaxTeachers = 10, UsedTeachers = 2 },
            new CommunityLicense { CommunityId = 2, MaxTeachers = 10 });
        foreach (var communityId in new long[] { 1, 2 })
            for (var grade = 7; grade <= 12; grade++)
                Context.Grades.Add(new Grade { Id = communityId * 100 + grade, CommunityId = communityId,
                    Value = grade, Name = $"Grade {grade}", SortOrder = grade });
        Context.Classes.AddRange(
            new Class { Id = 1, CommunityId = 1, GradeId = 107, Name = "Alpha" },
            new Class { Id = 2, CommunityId = 1, GradeId = 108, Name = "Beta" },
            new Class { Id = 3, CommunityId = 2, GradeId = 207, Name = "Other" },
            new Class { Id = 4, CommunityId = 1, GradeId = 107, Name = "Archived", Status = ClassStatus.Deleted });
        Context.TeacherClassAssignments.AddRange(
            new TeacherClassAssignment { ClassId = 1, TeacherUserId = 20 },
            new TeacherClassAssignment { ClassId = 1, TeacherUserId = 21 });
        for (var id = 1; id <= 3; id++)
        {
            Context.Players.Add(new Player { Id = id, Name = $"Student {id}", Email = $"student{id}@example.com" });
            Context.StudentLicenses.Add(new StudentLicense { Id = id, CommunityId = id == 3 ? 2 : 1, ClassId = id,
                GradeId = id == 3 ? 207 : id == 1 ? 107 : 108, PlayerProfileId = id, Email = $"student{id}@example.com",
                AssignedByUserId = id == 3 ? 30 : 10, Status = StudentLicenseStatus.Active });
        }
        await Context.SaveChangesAsync();
    }

    public UserManager<User> UserManager()
    {
        var store = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(Context);
        return new UserManager<User>(store, Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() }, new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), NullLogger<UserManager<User>>.Instance);
    }

    public void Dispose() => Context.Dispose();
}
