using Application.Features.Communities.Students.Common;
using MediatR;

namespace Application.Features.Communities.Students.StudentStats;

public class StudentStatsQueryHandler(StudentRosterService roster) : IRequestHandler<StudentStatsQuery, StudentStatsResponse>
{
    public async Task<StudentStatsResponse> Handle(StudentStatsQuery request, CancellationToken ct)
    {
        var rows = await roster.ReadAsync(request.UserId, request.ClassId, request.GradeId, request.Search, null, ct);
        return new StudentStatsResponse { Total = rows.Count, Active = rows.Count(x => x.Status == "ACTIVE"),
            Inactive = rows.Count(x => x.Status == "INACTIVE"), Pending = rows.Count(x => x.Status == "PENDING") };
    }
}
