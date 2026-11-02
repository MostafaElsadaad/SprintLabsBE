using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services;

public partial class QuestionDataService
{
    public Task<List<JourneyOptionResponse>> GetJourneyCatalogueAsync(CancellationToken ct) =>
        context.Set<Question>().AsNoTracking()
            .Select(q => new { q.Curriculum, q.Grade, q.Subject, q.Unit }).Distinct()
            .OrderBy(q => q.Curriculum).ThenBy(q => q.Grade).ThenBy(q => q.Subject).ThenBy(q => q.Unit)
            .Select(q => new JourneyOptionResponse { Curriculum = q.Curriculum, Grade = q.Grade,
                Subject = q.Subject, Unit = q.Unit }).ToListAsync(ct);
}
