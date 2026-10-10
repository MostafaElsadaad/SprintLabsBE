using MediatR;

namespace Application.Features.Communities.Students.StudentStats;

public class StudentStatsQuery : IRequest<StudentStatsResponse>
{
    public long UserId { get; set; }
    public long? ClassId { get; set; }
    public long? GradeId { get; set; }
    public string? Search { get; set; }
}
