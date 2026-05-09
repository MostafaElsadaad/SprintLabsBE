using Domain.Models;
using Domain.Repositories;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _context;

    public PlayerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByGoogleIdAsync(string googleId)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.GoogleId == googleId);
    }

    public async Task<Player> CreateAsync(Player player)
    {
        player.CreatedAt = DateTime.UtcNow;
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }

    public async Task<Player> UpdatePlayer (Player player)
    {
        player.UpdatedAt = DateTime.UtcNow;
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
        return player;
    }
}