using System.Globalization;
using Application.Features.CommunityDashboard.Common;
using Application.Features.Communities.Students.Common;
using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Communities.Students.ExportStudents;

public class ExportStudentsQueryHandler(StudentRosterService roster, IStudentRosterFileService files) : IRequestHandler<ExportStudentsQuery, StudentRosterDownload>
{
    public async Task<StudentRosterDownload> Handle(ExportStudentsQuery request, CancellationToken ct)
    {
        var format = request.Format.ToLowerInvariant();
        if (format is not ("csv" or "xlsx")) throw DashboardAuthorization.Invalid();
        var rows = await roster.ReadAsync(request.UserId, request.ClassId, request.GradeId, request.Search, request.Status, ct);
        if (rows.Count > 10000) throw DashboardAuthorization.Invalid();
        var content = rows.Select(x => new[] { x.StudentCode, x.FullName, x.Email, x.Status, x.LicenseStatus.ToUpperInvariant(),
            x.ClassId.ToString(CultureInfo.InvariantCulture), x.ClassName, x.Grade.Id.ToString(CultureInfo.InvariantCulture),
            x.Grade.Value.ToString(CultureInfo.InvariantCulture), x.Grade.Name, DateTime.SpecifyKind(x.JoinedAt, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            x.ActivatedAt.HasValue ? DateTime.SpecifyKind(x.ActivatedAt.Value, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture) : "", (x.AvgScore ?? 0).ToString(CultureInfo.InvariantCulture),
            (x.SessionsCount ?? 0).ToString(CultureInfo.InvariantCulture) }).ToList();
        return new StudentRosterDownload { Bytes = files.Write(new[] { "studentCode", "fullName", "email", "status", "licenseStatus",
            "classId", "className", "gradeId", "grade", "gradeName", "joinedAt", "activatedAt", "avgScore", "sessionsCount" }, content, format),
            ContentType = format == "csv" ? "text/csv; charset=utf-8" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"students-{DateTime.UtcNow:yyyyMMddHHmmss}.{format}" };
    }
}
