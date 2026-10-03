namespace Shared.Requests;

public class ProgressionPageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public long? GradeId { get; set; }
    public long? ClassId { get; set; }
}
