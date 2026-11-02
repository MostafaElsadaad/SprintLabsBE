using MediatR;
using Shared.Responses;
namespace Application.Features.Questions.GetJourneyCatalogue;
public sealed class GetJourneyCatalogueQuery : IRequest<List<JourneyOptionResponse>> { }
