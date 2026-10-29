using Domain.Models;
using Domain.Models.QuestionData;
using Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Features.PlayerProfiles.GetSetupOptions;

public class GetSetupOptionsQueryHandler(IBaseRepository<PlayerEducationOption> options,
    IBaseRepository<Question> questions) : IRequestHandler<GetSetupOptionsQuery, GetSetupOptionsResponse>
{
    public async Task<GetSetupOptionsResponse> Handle(GetSetupOptionsQuery request, CancellationToken cancellationToken)
    {
        var pairs = options.AsQueryable().AsNoTracking().Select(x => new { x.Curriculum, x.Language })
            .Union(questions.AsQueryable().AsNoTracking().Where(x => x.Grade >= 7 && x.Grade <= 12)
                .Select(x => new { x.Curriculum, x.Language }));
        var values = await pairs.ToListAsync(cancellationToken);
        return new GetSetupOptionsResponse
        {
            EducationOptions = values.Select(x => new EducationOptionDto { Curriculum = x.Curriculum.Trim(), Language = x.Language.Trim() })
                .Where(x => x.Curriculum.Length is > 0 and <= 120 && x.Language.Length is > 0 and <= 40)
                .DistinctBy(x => (x.Curriculum, x.Language)).OrderBy(x => x.Curriculum).ThenBy(x => x.Language).ToList()
        };
    }
}
