using System.Net;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.DevelopmentAuthentication;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Accounts.DevelopmentAuthentication.ListDevelopmentPlayers;

public sealed class ListDevelopmentPlayersQueryHandler : IRequestHandler<ListDevelopmentPlayersQuery, List<DevelopmentPlayerResponse>>
{
    private readonly IDevelopmentAuthenticationGuard _guard;
    private readonly IUserService _userService;
    private readonly IPlayerRepository _playerRepository;

    public ListDevelopmentPlayersQueryHandler(
        IDevelopmentAuthenticationGuard guard,
        IUserService userService,
        IPlayerRepository playerRepository)
    {
        _guard = guard;
        _userService = userService;
        _playerRepository = playerRepository;
    }

    public async Task<List<DevelopmentPlayerResponse>> Handle(
        ListDevelopmentPlayersQuery request,
        CancellationToken cancellationToken)
    {
        _guard.EnsureEndpointAccess(request.SuppliedApiKeys);
        var response = new List<DevelopmentPlayerResponse>(DevelopmentPlayerCatalog.All.Count);
        foreach (var definition in DevelopmentPlayerCatalog.All)
        {
            var user = await _userService.FindByEmail(definition.Email);
            if (user == null
                || user.IsSuspended
                || user.IsLockedOut
                || user.IsPlatformAdmin
                || user.IsTeacherAccount
                || !string.IsNullOrWhiteSpace(user.GoogleId)
                || !string.IsNullOrWhiteSpace(user.FirebaseUid))
            {
                throw InvalidSeedState();
            }

            var player = await _playerRepository.GetByUserIdAsync(user.Id);
            if (player == null
                || !user.PlayerProfileId.HasValue
                || !string.Equals(user.UserName, definition.AccountKey, StringComparison.Ordinal)
                || !string.Equals(user.Email, definition.Email, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(user.Name, definition.DisplayName, StringComparison.Ordinal)
                || user.PlayerProfileId.Value != player.Id
                || player.UserId != user.Id
                || !string.Equals(player.Email, definition.Email, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(player.Name, definition.DisplayName, StringComparison.Ordinal)
                || !string.IsNullOrWhiteSpace(player.GoogleId))
            {
                throw InvalidSeedState();
            }

            response.Add(new DevelopmentPlayerResponse
            {
                AccountKey = definition.AccountKey,
                DisplayName = definition.DisplayName
            });
        }

        return response;
    }

    private static GenericException InvalidSeedState() =>
        new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
}
