using System.Net;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Shared.DevelopmentAuthentication;
using Shared.Enums;
using Shared.Exceptions;
namespace Application.Features.Accounts.DevelopmentAuthentication.SeedJourneyDemo;
// Fixed synthetic catalogue only; ordinary accounts and competitive progression are never changed.
public sealed class SeedJourneyDemoCommandHandler(IDevelopmentAuthenticationGuard guard, IUserService users,
    IPlayerRepository players, IQuestionDataService questions)
    : IRequestHandler<SeedJourneyDemoCommand, SeedJourneyDemoResponse>
{
    public async Task<SeedJourneyDemoResponse> Handle(SeedJourneyDemoCommand request, CancellationToken ct)
    {
        guard.EnsureEndpointAccess(request.SuppliedApiKeys);
        var admin = await users.GetCurrentUser(request.AdminUserId);
        if (admin == null || !admin.IsPlatformAdmin || admin.IsSuspended || admin.IsLockedOut || admin.Status != "Active")
            throw Error(HttpStatusCode.Forbidden);
        var profiles = new List<Player>();
        foreach (var definition in DevelopmentPlayerCatalog.All)
        {
            ct.ThrowIfCancellationRequested();
            var user = await users.FindByEmail(definition.Email);
            if (user == null) throw Error(HttpStatusCode.NotFound);
            var player = await players.GetByUserIdAsync(user.Id);
            if (user.IsPlatformAdmin || user.IsTeacherAccount || user.IsSuspended || user.IsLockedOut || user.Status != "Active" ||
                !string.IsNullOrWhiteSpace(user.GoogleId) || !string.IsNullOrWhiteSpace(user.FirebaseUid) ||
                user.UserName != definition.AccountKey || !string.Equals(user.Email, definition.Email, StringComparison.OrdinalIgnoreCase) ||
                player == null || player.Id != user.PlayerProfileId || player.UserId != user.Id ||
                !string.IsNullOrWhiteSpace(player.GoogleId) || !string.Equals(player.Email, definition.Email, StringComparison.OrdinalIgnoreCase))
                throw Error(HttpStatusCode.Conflict);
            profiles.Add(player);
        }
        var content = JourneyDemoContent.Create();
        foreach (var question in content) await questions.PublishAsync(request.AdminUserId, question, ct);
        foreach (var player in profiles)
        {
            ct.ThrowIfCancellationRequested();
            if (player.Curriculum == JourneyDemoContent.Curriculum && player.Grade == JourneyDemoContent.Grade && player.Language == "en") continue;
            player.Curriculum = JourneyDemoContent.Curriculum; player.Grade = JourneyDemoContent.Grade; player.Language = "en";
            await players.UpdatePlayer(player);
        }
        return new(JourneyDemoContent.Curriculum, JourneyDemoContent.Grade, content.Count, profiles.Count);
    }
    private static GenericException Error(HttpStatusCode status) => new(ErrorCode.Failure, ErrorMessage.InvalidInput, status);
}

