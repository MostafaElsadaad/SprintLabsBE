using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

/// <summary>
/// Dashboard live updates for platform-admin live games. Clients call Watch(liveGameId) and receive
/// "LiveGameUpdated" with the full game snapshot after every change.
/// </summary>
[Authorize]
public class LiveGameHub : Hub
{
    public const string Path = "/hubs/live-games";
    public const string UpdatedMethod = "LiveGameUpdated";
    private readonly IUserService _users;

    public LiveGameHub(IUserService users) => _users = users;

    public static string GroupName(long liveGameId) => "live-game-" + liveGameId;

    public override async Task OnConnectedAsync()
    {
        if (!await IsPlatformAdminAsync())
        {
            Context.Abort();
            return;
        }
        await base.OnConnectedAsync();
    }

    public async Task Watch(long liveGameId)
    {
        if (liveGameId <= 0 || !await IsPlatformAdminAsync()) throw new HubException("Not allowed.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(liveGameId));
    }

    public Task Unwatch(long liveGameId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(liveGameId));

    private async Task<bool> IsPlatformAdminAsync()
    {
        if (!long.TryParse(Context.User?.FindFirst("userId")?.Value, out long userId) || userId <= 0) return false;
        var user = await _users.GetCurrentUser(userId);
        return user != null && !user.IsSuspended && user.IsPlatformAdmin;
    }
}
