using System.Net;
using System.Security.Claims;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.Common;

public class ExternalPlayerLoginWorkflow : IExternalPlayerLoginWorkflow
{
    private readonly IUserService _userService;
    private readonly IPlayerRepository _playerRepository;
    private readonly ICommunityLoginActivationService _communityLoginActivationService;

    public ExternalPlayerLoginWorkflow(IUserService userService, IPlayerRepository playerRepository, ICommunityLoginActivationService communityLoginActivationService)
    {
        _userService = userService;
        _playerRepository = playerRepository;
        _communityLoginActivationService = communityLoginActivationService;
    }

    public async Task<LoginResponse> CompleteAsync(UserIdentityResponse user, ExternalPlayerLoginContext context, CancellationToken cancellationToken)
    {
        if (user.IsSuspended)
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        }

        var player = await ResolvePlayerAsync(user.Id, context);
        if (context.EmailVerified)
        {
            await _communityLoginActivationService.ActivatePendingStudentLicensesAsync(user.Id, player.Id, context.Email, cancellationToken);
            if (context.ActivatePendingTeacherMemberships)
            {
                await _communityLoginActivationService.ActivateEligiblePendingTeacherMembershipsAsync(user.Id, context.Email, cancellationToken);
            }
        }

        return await IssueAsync(user, player, context);
    }

    public async Task<LoginResponse> CompleteExistingAsync(
        UserIdentityResponse user,
        Player player,
        ExternalPlayerLoginContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user.IsSuspended || user.IsLockedOut)
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        }

        if (player.UserId != user.Id || (user.PlayerProfileId.HasValue && user.PlayerProfileId.Value != player.Id))
        {
            throw Conflict();
        }

        return await IssueAsync(user, player, context);
    }

    private async Task<LoginResponse> IssueAsync(UserIdentityResponse user, Player player, ExternalPlayerLoginContext context)
    {
        var claims = new List<Claim>
        {
            new("email", context.Email),
            new("sub", context.Subject),
            new("name", context.Name),
            new("userId", user.Id.ToString()),
            new("playerProfileId", player.Id.ToString())
        };
        var response = await _userService.Authenticate(claims);
        response.UserId = user.Id;
        response.PlayerProfileId = player.Id;
        response.Email = context.Email;
        response.Name = context.Name;
        response.PictureUrl = context.PictureUrl;
        response.Gold = player.Gold;
        response.Experience = player.Experience;
        response.Level = player.Level;
        return response;
    }

    private async Task<Player> ResolvePlayerAsync(long userId, ExternalPlayerLoginContext context)
    {
        var player = await _playerRepository.GetByUserIdAsync(userId);
        if (player != null)
        {
            EnsureGoogleIdentityIsCompatible(player, context.GoogleProviderId);
            return player;
        }

        player = await _playerRepository.GetByGoogleIdAsync(context.GoogleProviderId);
        if (player == null && context.EmailVerified)
        {
            player = await _playerRepository.GetByEmailAsync(context.Email);
        }

        if (player != null)
        {
            if (player.UserId.HasValue && player.UserId.Value != userId)
            {
                throw Conflict();
            }

            EnsureGoogleIdentityIsCompatible(player, context.GoogleProviderId);
            player.UserId = userId;
            if (string.IsNullOrWhiteSpace(player.GoogleId) && !string.IsNullOrWhiteSpace(context.GoogleProviderId))
            {
                player.GoogleId = context.GoogleProviderId;
            }

            return await _playerRepository.UpdatePlayer(player);
        }

        try
        {
            return await _playerRepository.CreateAsync(new Player
            {
                UserId = userId,
                GoogleId = context.GoogleProviderId,
                Email = context.Email,
                Name = string.IsNullOrWhiteSpace(context.Name) ? context.Email : context.Name,
                AvatarUrl = context.PictureUrl
            });
        }
        catch (GenericException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            var concurrentPlayer = await _playerRepository.GetByUserIdAsync(userId);
            if (concurrentPlayer == null)
            {
                throw;
            }

            EnsureGoogleIdentityIsCompatible(concurrentPlayer, context.GoogleProviderId);
            return concurrentPlayer;
        }
    }

    private static void EnsureGoogleIdentityIsCompatible(Player player, string? googleProviderId)
    {
        if (!string.IsNullOrWhiteSpace(googleProviderId) && !string.IsNullOrWhiteSpace(player.GoogleId) && player.GoogleId != googleProviderId)
        {
            throw Conflict();
        }
    }

    private static GenericException Conflict() => new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
}
