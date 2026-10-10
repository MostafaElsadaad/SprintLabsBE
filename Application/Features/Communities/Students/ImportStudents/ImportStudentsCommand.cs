using MediatR;

namespace Application.Features.Communities.Students.ImportStudents;

public class ImportStudentsCommand : IRequest<ImportStudentsResponse>
{
    public long UserId { get; set; }
    public string FileName { get; set; } = "";
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
}
