using Application.Features.Admin.Communities.Common;
using Application.Features.TeacherQuestions.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.LiveGames;

namespace Application.Features.TeacherQuestions.ListTeacherQuestions;

public class ListTeacherQuestionsQueryHandler(IUserService users, IBaseRepository<TeacherQuestion> repository)
    : IRequestHandler<ListTeacherQuestionsQuery, List<TeacherQuestionResponse>>
{
    public async Task<List<TeacherQuestionResponse>> Handle(ListTeacherQuestionsQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var filter = request.Filter ?? new();
        var query = repository.AsQueryable().AsNoTracking().Where(q => q.OwnerUserId == request.AuthenticatedUserId);
        if (!string.IsNullOrWhiteSpace(filter.Curriculum)) query = query.Where(q => q.Curriculum == filter.Curriculum);
        if (filter.Grade.HasValue) query = query.Where(q => q.Grade == filter.Grade);
        if (filter.Unit.HasValue) query = query.Where(q => q.Unit == filter.Unit);
        if (filter.Lesson.HasValue) query = query.Where(q => q.Lesson == filter.Lesson);
        if (!string.IsNullOrWhiteSpace(filter.QuestionType)) query = query.Where(q => q.QuestionType == filter.QuestionType);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(q => q.QuestionText.Contains(search));
        }

        var rows = await query.OrderByDescending(q => q.UpdatedAt).Take(200).ToListAsync(cancellationToken);
        return rows.Select(TeacherQuestionMapper.Map).ToList();
    }
}
