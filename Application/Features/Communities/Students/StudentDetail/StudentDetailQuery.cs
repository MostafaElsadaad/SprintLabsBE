using MediatR;

namespace Application.Features.Communities.Students.StudentDetail;

public class StudentDetailQuery : IRequest<object>
{
    public long UserId { get; set; }
    public long StudentId { get; set; }
    public string IdType { get; set; } = "license";
}
