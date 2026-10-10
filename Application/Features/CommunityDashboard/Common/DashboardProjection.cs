using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Application.Features.CommunityDashboard.Common;

public class DashboardProjection
{
    private readonly IBaseRepository<Class> _classes;
    private readonly IBaseRepository<StudentLicense> _licenses;
    private readonly IBaseRepository<Player> _players;
    private readonly IBaseRepository<CommunityUser> _members;
    private readonly IBaseRepository<TeacherClassAssignment> _assignments;
    private readonly IBaseRepository<StaffActivity> _activity;
    private readonly IUserService _users;

    public DashboardProjection(IBaseRepository<Class> classes, IBaseRepository<StudentLicense> licenses,
        IBaseRepository<Player> players, IBaseRepository<CommunityUser> members,
        IBaseRepository<TeacherClassAssignment> assignments, IBaseRepository<StaffActivity> activity, IUserService users)
    {
        _classes = classes; _licenses = licenses; _players = players;
        _members = members; _assignments = assignments; _activity = activity; _users = users;
    }

    public async Task<List<StudentView>> StudentsAsync(DashboardScope scope, long? classId, CancellationToken ct, bool includeRevoked = false)
    {
        if (classId.HasValue) DashboardAuthorization.RequireClass(scope, classId.Value);
        var query = _licenses.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId &&
            scope.ClassIds.Contains(x.ClassId) && (includeRevoked || x.Status != StudentLicenseStatus.Revoked));
        if (classId.HasValue) query = query.Where(x => x.ClassId == classId.Value);
        return await (from license in query
                      join player in _players.AsQueryable().AsNoTracking() on license.PlayerProfileId equals (long?)player.Id into linked
                      from player in linked.DefaultIfEmpty()
                      select new StudentView
                      {
                          Id = license.Id, UserId = license.UserId, PlayerProfileId = license.PlayerProfileId,
                          ClassId = license.ClassId, Email = license.Email,
                          ClassName = license.Class.Name, Grade = new GradeSummary { Id = license.GradeId,
                              Value = license.Grade.Value ?? 0, Name = license.Grade.Name },
                          JoinedAt = license.CreatedAt, ActivatedAt = license.ActivatedAt,
                          FullName = player != null ? player.Name : license.Email,
                          LicenseStatus = license.Status.ToString()
                      }).ToListAsync(ct);
    }

    public async Task<List<ClassView>> ClassesAsync(DashboardScope scope, CancellationToken ct)
    {
        var classes = await _classes.AsQueryable().AsNoTracking().Include(x => x.Grade)
            .Where(x => x.CommunityId == scope.CommunityId && scope.ClassIds.Contains(x.Id)).ToListAsync(ct);
        var ids = classes.Select(x => x.Id).ToList();
        var assignments = await _assignments.AsQueryable().AsNoTracking()
            .Where(x => ids.Contains(x.ClassId) && _members.AsQueryable().Any(m => m.CommunityId == scope.CommunityId &&
                m.UserId == x.TeacherUserId && m.Role == CommunityUserRole.Teacher && m.Status == CommunityUserStatus.Active)).ToListAsync(ct);
        var teachers = (await _users.GetUsersByIds(assignments.Select(x => x.TeacherUserId).Distinct(), ct))
            .ToDictionary(x => x.Id);
        var counts = await _licenses.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId &&
            ids.Contains(x.ClassId) && x.Status != StudentLicenseStatus.Revoked)
            .GroupBy(x => x.ClassId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return classes.Select(x => new ClassView
        {
            Id = x.Id, Name = x.Name, Grade = new GradeSummary { Id = x.GradeId, Value = x.Grade.Value ?? 0, Name = x.Grade.Name },
            Teachers = assignments.Where(a => a.ClassId == x.Id && teachers.ContainsKey(a.TeacherUserId))
                .OrderBy(a => a.TeacherUserId).Select(a => new TeacherSummary { Id = a.TeacherUserId, FullName = teachers[a.TeacherUserId].Name }).ToList(),
            StudentsCount = counts.GetValueOrDefault(x.Id), CreatedAt = x.CreatedAt,
            Status = x.Status == ClassStatus.Deleted ? "ARCHIVED" :
                assignments.Any(a => a.ClassId == x.Id) ? "ACTIVE" : "NO_TEACHER"
        }).ToList();
    }

    public async Task<List<TeacherView>> TeachersAsync(DashboardScope scope, CancellationToken ct)
    {
        if (!scope.IsOwner) throw DashboardAuthorization.Forbidden();
        var memberships = await _members.AsQueryable().AsNoTracking()
            .Where(x => x.CommunityId == scope.CommunityId && x.Role == CommunityUserRole.Teacher).ToListAsync(ct);
        var users = (await _users.GetUsersByIds(memberships.Select(x => x.UserId), ct)).ToDictionary(x => x.Id);
        var classes = await ClassesAsync(scope, ct);
        var ids = memberships.Select(x => x.UserId).ToList();
        var assignments = await _assignments.AsQueryable().AsNoTracking().Where(x => ids.Contains(x.TeacherUserId) &&
            x.Class.CommunityId == scope.CommunityId && x.Class.Status == ClassStatus.Active).ToListAsync(ct);
        var activity = await _activity.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId &&
            ids.Contains(x.TeacherUserId)).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
        return memberships.Where(x => users.ContainsKey(x.UserId)).Select(m =>
        {
            var assignedIds = assignments.Where(a => a.TeacherUserId == m.UserId).Select(a => a.ClassId).ToHashSet();
            var assignedClasses = m.Status == CommunityUserStatus.Active ? classes.Where(c => assignedIds.Contains(c.Id)).ToList() : new List<ClassView>();
            var events = activity.Where(a => a.TeacherUserId == m.UserId).ToList();
            var recent = events.Select(a => new ActivityView { Label = a.Label, At = a.CreatedAt })
                .Append(new ActivityView { Label = "Joined community", At = m.CreatedAt })
                .OrderByDescending(a => a.At).Take(3).ToList();
            return new TeacherView
            {
                Id = m.UserId, FullName = users[m.UserId].Name, Email = users[m.UserId].Email,
                TeacherCode = $"TCH-{m.UserId:D3}", Title = m.TeacherTitle,
                MembershipStatus = m.Status.ToString(),
                Status = m.Status == CommunityUserStatus.Pending ? "PENDING" : m.Status == CommunityUserStatus.Active ? "ACTIVE" : "INACTIVE",
                Classes = assignedClasses,
                Grades = assignedClasses.Select(c => c.Grade).DistinctBy(g => g.Id).OrderBy(g => g.Value).ToList(),
                StudentsCount = m.Status == CommunityUserStatus.Pending ? null : assignedClasses.Sum(c => c.StudentsCount),
                JoinedAt = m.CreatedAt, LastObservedActivityAt = events.FirstOrDefault()?.CreatedAt,
                LicenseStatus = m.Status == CommunityUserStatus.Removed ? "NOT_ASSIGNED" : "ASSIGNED", RecentActivity = recent
            };
        }).ToList();
    }
}
