using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByGoogleIdAsync(string googleId);
    Task<Player> CreateAsync(Player player);
    Task<Player> UpdatePlayer(Player player);
}