using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerRepository
{
    public Task<Player?> GetByGoogleIdAsync(string googleId);
    public Task<Player?> GetByUserIdAsync(long userId);
    public Task<Player> CreateAsync(Player player);
    public Task<Player> UpdatePlayer(Player player);
}