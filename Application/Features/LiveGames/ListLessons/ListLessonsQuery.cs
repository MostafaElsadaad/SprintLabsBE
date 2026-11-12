using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.ListLessons;

/// <summary>Lessons of one curriculum/grade/unit (optionally subject) with their library question counts.</summary>
public class ListLessonsQuery : IRequest<List<LessonOptionResponse>>
{
    public long AuthenticatedUserId { get; set; }
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int Unit { get; set; }
}
