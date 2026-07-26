using System.Security.Claims;

using Application.Features.Accounts.GoogleAuthenticate;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Responses;

namespace Compass.Tests.Features.StudentLicenseActivation;

public class StudentLicenseActivationOnLoginTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IGoogleAuthenticationService> _googleAuthenticationServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    [Fact]
    public async Task Handle_MatchingPendingStudentLicense_ActivatesLicenseAndCreatesStudentMembership()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "Student@Example.com");
        SetupGoogleLogin(email: " student@example.com ");
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
        result.UserId.Should().Be(20);
        result.PlayerProfileId.Should().Be(30);

        var studentLicense = await context.StudentLicenses.SingleAsync();
        studentLicense.Status.Should().Be(StudentLicenseStatus.Active);
        studentLicense.UserId.Should().Be(20);
        studentLicense.PlayerProfileId.Should().Be(30);
        studentLicense.ActivatedAt.Should().NotBeNull();
        studentLicense.UpdatedAt.Should().NotBeNull();

        var membership = await context.CommunityUsers.SingleAsync();
        membership.CommunityId.Should().Be(1);
        membership.UserId.Should().Be(20);
        membership.Role.Should().Be(CommunityUserRole.Student);
        membership.Status.Should().Be(CommunityUserStatus.Active);

        var license = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        license.UsedStudents.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MatchingPendingStudentLicensesAcrossCommunities_ActivatesAll()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com", 1, 1, 1);
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com", 2, 2, 2);
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        var studentLicenses = await context.StudentLicenses.ToListAsync();
        studentLicenses.Should().HaveCount(2);
        studentLicenses.Should().OnlyContain(x =>
            x.Status == StudentLicenseStatus.Active
            && x.UserId == 20
            && x.PlayerProfileId == 30
            && x.ActivatedAt != null);

        var memberships = await context.CommunityUsers
            .Where(x => x.UserId == 20 && x.Role == CommunityUserRole.Student)
            .ToListAsync();
        memberships.Should().HaveCount(2);
        memberships.Select(x => x.CommunityId).Should().BeEquivalentTo(new long[] { 1, 2 });

        var licenses = await context.CommunityLicenses.ToListAsync();
        licenses.Should().OnlyContain(x => x.UsedStudents == 1);
    }

    [Fact]
    public async Task Handle_RemovedStudentMembership_RestoresExistingMembership()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com");
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Student,
            Status = CommunityUserStatus.Removed
        });
        await context.SaveChangesAsync();
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        var membership = await context.CommunityUsers.SingleAsync();
        membership.Status.Should().Be(CommunityUserStatus.Active);
        membership.Role.Should().Be(CommunityUserRole.Student);
        membership.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NoPendingStudentLicenses_PreservesExistingLoginResponse()
    {
        await using var context = CreateContext();
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
        result.UserId.Should().Be(20);
        result.PlayerProfileId.Should().Be(30);
        result.Email.Should().Be("student@example.com");
        result.Name.Should().Be("Student Name");
        result.PictureUrl.Should().Be("avatar.png");
        result.Gold.Should().Be(5);
        result.Experience.Should().Be(50);
        result.Level.Should().Be(2);

        (await context.StudentLicenses.CountAsync()).Should().Be(0);
        (await context.CommunityUsers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_PendingTeacherAndStudentRecords_ActivatesOnlyStudentWithoutChangingUsageCounts()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com");
        context.Communities.Add(new Community { Id = 2, Name = "Two", Slug = "two" });
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 2,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Pending
        });
        context.CommunityLicenses.Add(new CommunityLicense
        {
            CommunityId = 2,
            MaxTeachers = 5,
            UsedTeachers = 1
        });
        await context.SaveChangesAsync();
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        var teacherMembership = await context.CommunityUsers.SingleAsync(x => x.Role == CommunityUserRole.Teacher);
        teacherMembership.Status.Should().Be(CommunityUserStatus.Pending);

        var studentMembership = await context.CommunityUsers.SingleAsync(x => x.Role == CommunityUserRole.Student);
        studentMembership.Status.Should().Be(CommunityUserStatus.Active);

        var studentCommunityLicense = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        studentCommunityLicense.UsedStudents.Should().Be(1);

        var teacherCommunityLicense = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 2);
        teacherCommunityLicense.UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RepeatedLogin_DoesNotDuplicateMembershipOrChangeUsedStudents()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com");
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);
        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        (await context.CommunityUsers.CountAsync(x =>
            x.CommunityId == 1
            && x.UserId == 20
            && x.Role == CommunityUserRole.Student)).Should().Be(1);

        var studentLicense = await context.StudentLicenses.SingleAsync();
        studentLicense.Status.Should().Be(StudentLicenseStatus.Active);

        var license = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        license.UsedStudents.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RevokedAndActiveStudentLicenses_DoesNotReactivateThem()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com", status: StudentLicenseStatus.Revoked);
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com", 2, 2, 2, StudentLicenseStatus.Active);
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        var studentLicenses = await context.StudentLicenses.ToListAsync();
        studentLicenses.Should().ContainSingle(x =>
            x.CommunityId == 1
            && x.Status == StudentLicenseStatus.Revoked
            && x.UserId == null
            && x.PlayerProfileId == null
            && x.ActivatedAt == null);
        studentLicenses.Should().ContainSingle(x =>
            x.CommunityId == 2
            && x.Status == StudentLicenseStatus.Active
            && x.UserId == null
            && x.PlayerProfileId == null
            && x.ActivatedAt == null);
        (await context.CommunityUsers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExistingNonStudentMembership_DoesNotOverwriteRoleDuplicateMembershipOrActivateLicense()
    {
        await using var context = CreateContext();
        await SeedCommunityLicenseGradeClassAndPendingLicense(context, "student@example.com");
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Teacher,
            Status = CommunityUserStatus.Active
        });
        await context.SaveChangesAsync();
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        var membership = await context.CommunityUsers.SingleAsync();
        membership.Role.Should().Be(CommunityUserRole.Teacher);
        membership.Status.Should().Be(CommunityUserStatus.Active);

        var studentLicense = await context.StudentLicenses.SingleAsync();
        studentLicense.Status.Should().Be(StudentLicenseStatus.Pending);
        studentLicense.UserId.Should().BeNull();
        studentLicense.PlayerProfileId.Should().BeNull();
        studentLicense.ActivatedAt.Should().BeNull();
    }

    private GoogleAuthenticationCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var activationService = new CommunityLoginActivationService(
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<StudentLicense>(context));

        return new GoogleAuthenticationCommandHandler(
            _userServiceMock.Object,
            _googleAuthenticationServiceMock.Object,
            _playerRepositoryMock.Object,
            activationService);
    }

    private void SetupGoogleLogin(
        string email = "student@example.com",
        long userId = 20,
        long playerId = 30)
    {
        _googleAuthenticationServiceMock
            .Setup(x => x.GetUserInfo("google-token"))
            .ReturnsAsync(new GoogleUserResponse
            {
                Sub = "google-sub",
                Email = email,
                Name = "Student Name",
                Picture = "avatar.png"
            });

        _googleAuthenticationServiceMock
            .Setup(x => x.GenerateGoogleClaims(It.IsAny<GoogleUserResponse>()))
            .Returns(new List<Claim>());

        _userServiceMock
            .Setup(x => x.FindOrCreateGoogleUser(It.IsAny<GoogleUserResponse>()))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = userId,
                Email = email.Trim(),
                Name = "Student Name",
                Status = "Active"
            });

        _userServiceMock
            .Setup(x => x.Authenticate(It.IsAny<List<Claim>>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt-token" });

        _playerRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new Player
            {
                Id = playerId,
                UserId = userId,
                GoogleId = "google-sub",
                Email = email.Trim(),
                Name = "Student Name",
                AvatarUrl = "avatar.png",
                Gold = 5,
                Experience = 50,
                Level = 2
            });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedCommunityLicenseGradeClassAndPendingLicense(
        ApplicationDbContext context,
        string email,
        long communityId = 1,
        long gradeId = 1,
        long classId = 1,
        StudentLicenseStatus status = StudentLicenseStatus.Pending)
    {
        if (!await context.Communities.AnyAsync(x => x.Id == communityId))
        {
            context.Communities.Add(new Community
            {
                Id = communityId,
                Name = $"Community {communityId}",
                Slug = $"community-{communityId}"
            });
        }

        if (!await context.Grades.AnyAsync(x => x.Id == gradeId))
        {
            context.Grades.Add(new Grade
            {
                Id = gradeId,
                CommunityId = communityId,
                Name = $"Grade {gradeId}",
                SortOrder = (int)gradeId
            });
        }

        if (!await context.Classes.AnyAsync(x => x.Id == classId))
        {
            context.Classes.Add(new Class
            {
                Id = classId,
                CommunityId = communityId,
                GradeId = gradeId,
                Name = $"Class {classId}",
                Status = ClassStatus.Active
            });
        }

        if (!await context.CommunityLicenses.AnyAsync(x => x.CommunityId == communityId))
        {
            context.CommunityLicenses.Add(new CommunityLicense
            {
                CommunityId = communityId,
                MaxStudents = 10,
                UsedStudents = 1
            });
        }

        context.StudentLicenses.Add(new StudentLicense
        {
            CommunityId = communityId,
            Email = email.Trim().ToLowerInvariant(),
            GradeId = gradeId,
            ClassId = classId,
            Status = status,
            AssignedByUserId = 1
        });

        await context.SaveChangesAsync();
    }
}
