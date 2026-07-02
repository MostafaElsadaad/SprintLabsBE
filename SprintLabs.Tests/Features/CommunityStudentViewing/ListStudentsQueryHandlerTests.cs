using System.Net;

using Application.Features.Communities.Students.ListStudents;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Moq;

using Shared.Exceptions;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.CommunityStudentViewing;

public class ListStudentsQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_Owner_ReturnsPagedRouteCommunityStudentsWithNullablePendingFields()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1,
            PageNumber = 1,
            PageSize = 10
        }, CancellationToken.None);

        result.TotalRecords.Should().Be(3);
        result.Data.Select(x => x.Email).Should().NotContain("other.student@example.com");
        var pending = result.Data.Single(x => x.Email == "pending.student@example.com");
        pending.UserId.Should().BeNull();
        pending.PlayerProfileId.Should().BeNull();
        pending.PlayerName.Should().BeNull();
        pending.GradeName.Should().Be("Grade 5");
        pending.ClassName.Should().Be("Class A");
    }

    [Fact]
    public async Task Handle_Teacher_ReturnsStudents()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context, CommunityUserRole.Teacher);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.TotalRecords.Should().Be(3);
    }

    [Fact]
    public async Task Handle_AppliesStatusGradeClassSearchAndPagination()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1,
            Status = StudentLicenseStatus.Active,
            GradeId = 1,
            ClassId = 1,
            Search = "Active Player",
            PageNumber = 1,
            PageSize = 1
        }, CancellationToken.None);

        result.TotalRecords.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Data.Should().ContainSingle();
        result.Data.Single().Email.Should().Be("active.student@example.com");
        result.Data.Single().PlayerName.Should().Be("Active Player");
    }

    [Theory]
    [InlineData(3L, null)]
    [InlineData(null, 3L)]
    [InlineData(2L, 1L)]
    [InlineData(null, 4L)]
    public async Task Handle_InvalidGradeOrClassFilter_ThrowsNotFound(long? gradeId, long? classId)
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = gradeId,
            ClassId = classId
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Removed)]
    public async Task Handle_DisallowedMembership_ThrowsForbidden(
        CommunityUserRole role,
        CommunityUserStatus status)
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context, role, status);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Handle_NoMembership_ThrowsForbidden()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context, role: null);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListStudentsQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private ListStudentsQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new ListStudentsQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context),
            new BaseRepository<Player>(context));
    }
}
