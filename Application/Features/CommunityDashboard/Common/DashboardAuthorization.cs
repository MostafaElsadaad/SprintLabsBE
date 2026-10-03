using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.CommunityDashboard.Common;

public class DashboardAuthorization
{
    private readonly IUserService _users;
    private readonly ICommunityAccessService _access;
    private readonly IBaseRepository<TeacherClassAssignment> _assignments;
    private readonly IBaseRepository<Class> _classes;

    public DashboardAuthorization(IUserService users, ICommunityAccessService access,
        IBaseRepository<TeacherClassAssignment> assignments, IBaseRepository<Class> classes)
    {
        _users = users;
        _access = access;
        _assignments = assignments;
        _classes = classes;
    }

    public async Task<DashboardScope> ResolveAsync(long userId, bool ownerOnly, CancellationToken ct)
    {
        var user = userId > 0 ? await _users.GetCurrentUser(userId) : null;
        if (user == null || user.IsSuspended) throw Forbidden();
        var communityId = await _access.ResolveCurrentStaffCommunityId(userId, ct);
        if (!communityId.HasValue) throw Forbidden();
        var owner = await _access.HasCommunityRole(userId, communityId.Value, new[] { CommunityUserRole.Owner });
        if (ownerOnly && !owner) throw Forbidden();
        if (!owner && !await _access.HasCommunityRole(userId, communityId.Value, new[] { CommunityUserRole.Teacher })) throw Forbidden();
        var classes = _classes.AsQueryable().Where(x => x.CommunityId == communityId.Value);
        if (!owner)
            classes = classes.Where(x => x.Status == ClassStatus.Active &&
                _assignments.AsQueryable().Any(a => a.ClassId == x.Id && a.TeacherUserId == userId));
        return new DashboardScope(userId, communityId.Value, owner, await classes.Select(x => x.Id).ToListAsync(ct));
    }

    public static void RequireClass(DashboardScope scope, long classId)
    {
        if (classId <= 0 || !scope.ClassIds.Contains(classId)) throw NotFound();
    }

    public static GenericException Invalid() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
    public static GenericException NotFound() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
    public static GenericException Forbidden() => new(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
    public static GenericException Conflict() => new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
}
