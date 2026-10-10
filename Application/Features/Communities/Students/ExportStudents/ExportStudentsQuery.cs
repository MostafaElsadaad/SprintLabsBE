using MediatR;
using Shared.Responses;

namespace Application.Features.Communities.Students.ExportStudents;

public class ExportStudentsQuery : IRequest<StudentRosterDownload>
{
    public long UserId { get; set; }
    public long? ClassId { get; set; }
    public long? GradeId { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string Format { get; set; } = "csv";
}
