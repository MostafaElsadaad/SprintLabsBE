using System.Net;

using Application.Features.Accounts.Common;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.DevelopmentAuthentication;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.DevelopmentAuthentication.DevelopmentLogin;

public sealed class DevelopmentLoginCommandHandler : IRequestHandler<DevelopmentLoginCommand, LoginResponse>
{
    private readonly IDevelopmentAuthenticationGuard _guard;
    private readonly IUserService _userService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IExternalPlayerLoginWorkflow _externalPlayerLoginWorkflow;

    public DevelopmentLoginCommandHandler(
        IDevelopmentAuthenticationGuard guard,
        IUserService userService,
        IPlayerRepository playerRepository,
        IExternalPlayerLoginWorkflow externalPlayerLoginWorkflow)
    {
        _guard = guard;
        _userService = userService;
        _playerRepository = playerRepository;
        _externalPlayerLoginWorkflow = externalPlayerLoginWorkflow;
    }

    public async Task<LoginResponse> Handle(DevelopmentLoginCommand request, CancellationToken cancellationToken)
    {
        _guard.EnsureEndpointAccess(request.SuppliedApiKeys);
        ValidateRequestKey(request.AccountKey);
        if (!DevelopmentPlayerCatalog.TryGet(request.AccountKey, out var definition))
        {
            throw NotFound();
        }

        var user = await _userService.FindByEmail(definition.Email);
        if (user == null)
        {
            throw NotFound();
        }

        if (user.IsSuspended || user.IsLockedOut)
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        }

        if (user.IsPlatformAdmin
            || user.IsTeacherAccount
            || !string.IsNullOrWhiteSpace(user.GoogleId)
            || !string.IsNullOrWhiteSpace(user.FirebaseUid))
        {
            throw Conflict();
        }

        var player = await _playerRepository.GetByUserIdAsync(user.Id);
        if (player == null || !user.PlayerProfileId.HasValue)
        {
            throw Conflict();
        }

        if (!string.Equals(user.UserName, definition.AccountKey, StringComparison.Ordinal)
            || !string.Equals(user.Email, definition.Email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.Name, definition.DisplayName, StringComparison.Ordinal)
            || user.PlayerProfileId.Value != player.Id
            || player.UserId != user.Id
            || !string.Equals(player.Email, definition.Email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(player.Name, definition.DisplayName, StringComparison.Ordinal)
            || !string.IsNullOrWhiteSpace(player.GoogleId))
        {
            throw Conflict();
        }

        return await _externalPlayerLoginWorkflow.CompleteExistingAsync(
            user,
            player,
            new ExternalPlayerLoginContext
            {
                Subject = definition.AccountKey,
                Email = user.Email,
                EmailVerified = false,
                Name = user.Name,
                PictureUrl = user.AvatarUrl ?? player.AvatarUrl ?? string.Empty,
                GoogleProviderId = null,
                ActivatePendingTeacherMemberships = false
            },
            cancellationToken);
    }

    private static void ValidateRequestKey(string accountKey)
    {
        if (string.IsNullOrWhiteSpace(accountKey)
            || !string.Equals(accountKey, accountKey.Trim(), StringComparison.Ordinal)
            || !accountKey.StartsWith("dev-player-", StringComparison.Ordinal))
        {
            throw new GenericException(ErrorCode.ValidationError, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        }
    }

    private static GenericException NotFound() =>
        new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

    private static GenericException Conflict() =>
        new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
}
