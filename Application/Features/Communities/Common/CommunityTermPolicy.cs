using System.Net;
using Domain.Models;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Communities.Common;

public static class CommunityTermPolicy
{
    public static CommunityTermResponse? Read(Community community)
    {
        if (string.IsNullOrWhiteSpace(community.TermName) || community.TermName.Length > 100 ||
            !community.TermStartDate.HasValue || !community.TermEndDate.HasValue ||
            community.TermEndDate < community.TermStartDate || community.TermStartDate < new DateOnly(1000, 1, 1) ||
            community.TermEndDate == DateOnly.MaxValue) return null;
        return new CommunityTermResponse { Name = community.TermName, StartDate = community.TermStartDate.Value, EndDate = community.TermEndDate.Value };
    }

    public static decimal? Progress(CommunityTermResponse? term, DateTimeOffset asOf)
    {
        if (term == null) return null;
        var start = term.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = term.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var elapsed = (asOf.UtcDateTime - start).Ticks;
        var duration = (endExclusive - start).Ticks;
        return Math.Round(Math.Clamp(elapsed * 100m / duration, 0m, 100m), 2, MidpointRounding.AwayFromZero);
    }

    public static void Apply(Community community, CommunityTermRequest? input)
    {
        if (input == null) return;
        if (input.Name == null && input.StartDate == null && input.EndDate == null)
        { community.TermName = null; community.TermStartDate = null; community.TermEndDate = null; return; }
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100 || !input.StartDate.HasValue ||
            !input.EndDate.HasValue || input.StartDate < new DateOnly(1000, 1, 1) || input.EndDate < input.StartDate || input.EndDate == DateOnly.MaxValue)
            throw new GenericException(ErrorCode.ValidationError, "Provide term name (1-100 characters) and valid inclusive start/end dates.", HttpStatusCode.BadRequest);
        community.TermName = input.Name.Trim(); community.TermStartDate = input.StartDate; community.TermEndDate = input.EndDate;
    }
}
