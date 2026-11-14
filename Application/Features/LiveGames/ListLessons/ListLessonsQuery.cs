using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.ListLessons;

/// <summary>Lessons of one curriculum/grade and one or more units (optionally some subjects) with their library question counts.</summary>
public class ListLessonsQuery : IRequest<List<LessonOptionResponse>>
{
    public long AuthenticatedUserId { get; set; }
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public List<string> Subjects { get; set; } = new();
    public List<int> Units { get; set; } = new();
}
