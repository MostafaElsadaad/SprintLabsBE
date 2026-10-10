using Application.Features.CommunityDashboard.StaffDashboard;
using Application.Features.CommunityDashboard.StaffIdentity;
using Application.Features.CommunityDashboard.TeacherActivity;
using Shared.Responses;

namespace Application.Features.CommunityDashboard.Common;

public static class DashboardResponseMapper
{
    public static object Identity(StaffIdentityResponse row) => new
    {
        row.Id, row.FullName, row.Role, row.Title, row.UnreadNotificationsCount
    };

    public static object Dashboard(StaffDashboardResponse row)
    {
        if (row.Role == "TEACHER") return new
        {
            ClassesCount = row.ActiveClasses, StudentsCount = row.TotalStudents,
            SessionsThisWeek = row.SessionsThisWeek ?? 0,
            SessionsChangeVsLastWeekPercent = row.SessionsChangeVsLastWeekPercent ?? 0,
            GameplayHoursThisWeek = row.GameplayHoursThisWeek ?? 0,
            AvgHoursPerStudent = row.AvgHoursPerStudent ?? 0,
            Classes = row.Classes.Select(x => new
            {
                x.Id, x.Name, Grade = x.Grade.Value, x.StudentsCount, NextSession = (DateTime?)null
            })
        };
        return new
        {
            row.TotalStudents, row.NewStudentsThisWeek, row.ActiveClasses, row.GradesCount,
            TeachersCount = row.TeachersCount ?? 0, PendingInvitationsCount = row.PendingInvitationsCount ?? 0,
            GameplayHours = row.GameplayHours ?? 0,
            GameplayHoursChangeVsPreviousMonthPercent = row.GameplayHoursChangeVsPreviousMonthPercent ?? 0
        };
    }

    public static object Grade(GradeView row) => new { row.Id, row.Name, row.ClassesCount, row.StudentsCount };

    public static object Class(ClassView row) => new
    {
        row.Id, row.Name, Grade = new { row.Grade.Id, row.Grade.Name },
        row.Teachers, row.StudentsCount, row.Status
    };

    public static object ClassDetail(ClassDetail row)
    {
        if (!row.IsOwner) return new
        {
            row.Id, row.Name, Grade = row.Grade.Value, row.TeacherName, row.CreatedAt,
            row.StudentsCount, AvgScore = row.AvgScore ?? 0, InactiveStudentsCount = row.InactiveStudentsCount ?? 0
        };
        return new
        {
            row.Id, row.Name, Grade = new { row.Grade.Id, row.Grade.Name }, row.Status, row.Teachers,
            row.StudentsCount, AvgScore = row.AvgScore ?? 0, row.CreatedAt, row.LastActiveAt,
            StudentsPreview = row.StudentsPreview.Select(x => new { x.Id, x.FullName })
        };
    }

    public static object Student(StudentView row) => new
    {
        row.Id, row.FullName, AvgScore = row.AvgScore ?? 0, SessionsCount = row.SessionsCount ?? 0,
        row.Status, row.StudentCode, row.Email, Class = new { Id = row.ClassId, Name = row.ClassName },
        row.Grade, LicenseStatus = row.LicenseStatus.ToUpperInvariant(), row.JoinedAt, row.ActivatedAt
    };

    private static string? TeacherGrade(TeacherView row) => row.Grades.Count == 0 ? null :
        string.Join(", ", row.Grades.Select(x => x.Name));

    public static object Teacher(TeacherView row) => new
    {
        row.Id, row.FullName, row.Title, row.TeacherCode, Grade = TeacherGrade(row),
        Classes = row.Classes.Select(x => new { x.Id, x.Name }),
        StudentsCount = row.StudentsCount ?? 0, row.JoinedAt, row.Status
    };

    public static object TeacherDetail(TeacherView row) => new
    {
        row.Id, row.FullName, row.TeacherCode, row.Title, row.Status, row.Email, Grade = TeacherGrade(row),
        StudentsCount = row.StudentsCount ?? 0, row.JoinedAt, LastActiveAt = row.LastObservedActivityAt,
        row.LicenseStatus, Classes = row.Classes.Select(x => new { x.Id, x.Name, x.StudentsCount }), row.RecentActivity
    };

    public static object TeacherActivity(TeacherActivityResponse row) => new
    {
        row.TeacherId, row.FullName, row.ClassesCount, TotalSessions = row.TotalSessions ?? 0, row.LastPlayedAt
    };

    public static object Page<T>(PagedResponse<T> page, Func<T, object> map) => new
    {
        Items = page.Data.Select(map), Page = page.PageNumber, page.PageSize, Total = page.TotalRecords
    };
}
