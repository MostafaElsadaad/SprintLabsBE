using Domain.Services;
using MediatR;
using Shared.Responses;
namespace Application.Features.Questions.GetJourneyCatalogue;
public sealed class GetJourneyCatalogueQueryHandler(IQuestionDataService service)
    : IRequestHandler<GetJourneyCatalogueQuery, List<JourneyOptionResponse>>
{
    public Task<List<JourneyOptionResponse>> Handle(GetJourneyCatalogueQuery request, CancellationToken ct)
        => service.GetJourneyCatalogueAsync(ct);
}
