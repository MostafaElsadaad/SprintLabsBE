using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Domain.Services;
using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.ListLessons;

public class ListLessonsQueryHandler(IUserService users, IQuestionDataService questions)
    : IRequestHandler<ListLessonsQuery, List<LessonOptionResponse>>
{
    public async Task<List<LessonOptionResponse>> Handle(ListLessonsQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        string curriculum = (request.Curriculum ?? "").Trim();
        if (curriculum.Length == 0 || request.Grade < 1 || request.Unit < 1)
            throw LiveGameQuestionNormalizer.BadRequest("Choose a curriculum, grade and unit.");
        string? subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim();
        return await questions.GetLessonsAsync(curriculum, request.Grade, subject, request.Unit, cancellationToken);
    }
}
