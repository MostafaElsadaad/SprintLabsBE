using Domain.Models;
using Domain.Repositories;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _context;

    public PlayerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByGoogleIdAsync(string? googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
        {
            return null;
        }

        return await _context.Players
            .FirstOrDefaultAsync(p => p.GoogleId == googleId);
    }

    public async Task<Player?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        return await _context.Players
            .FirstOrDefaultAsync(p => p.Email.ToLower() == normalizedEmail);
    }

    public async Task<Player?> GetByUserIdAsync(long userId)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Player> CreateAsync(Player player)
    {
        player.CreatedAt = DateTime.UtcNow;
        _context.Players.Add(player);
        try
        {
            await _context.SaveChangesAsync();
            return player;
        }
        catch (DbUpdateException)
        {
            _context.Entry(player).State = EntityState.Detached;
            throw new GenericException(ErrorCode.Failure, ErrorMessage.ExistingRecord, System.Net.HttpStatusCode.Conflict);
        }
    }

    public async Task<Player> UpdatePlayer(Player player)
    {
        player.UpdatedAt = DateTime.UtcNow;
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
        return player;
    }
}
