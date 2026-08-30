using System.Data;
using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Shared.Enums;
using Shared.Exceptions;

namespace Infrastructure.Services;

public class StaffCommunityMembershipService : IStaffCommunityMembershipService
{
    private readonly ApplicationDbContext _context;

    public StaffCommunityMembershipService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommunityUser> AssignOwnerAsync(
        long userId,
        long communityId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        await StaffCommunityMembershipIntegrity.LockUserAsync(_context, userId, cancellationToken);
        if (await StaffCommunityMembershipIntegrity.HasCurrentStaffMembershipInAnotherCommunityAsync(
                _context,
                userId,
                communityId,
                cancellationToken))
        {
            throw Conflict();
        }

        var membership = await _context.CommunityUsers.FirstOrDefaultAsync(
            x => x.CommunityId == communityId && x.UserId == userId,
            cancellationToken);
        var now = DateTime.UtcNow;
        if (membership == null)
        {
            membership = new CommunityUser
            {
                CommunityId = communityId,
                UserId = userId,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Active,
                CreatedAt = now
            };
            _context.CommunityUsers.Add(membership);
        }
        else if (membership.Status != CommunityUserStatus.Removed &&
                 membership.Role != CommunityUserRole.Owner)
        {
            throw Conflict();
        }
        else
        {
            membership.Role = CommunityUserRole.Owner;
            membership.Status = CommunityUserStatus.Active;
            membership.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return membership;
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    private static GenericException Conflict()
    {
        return new GenericException(
            ErrorCode.Failure,
            ErrorMessage.ExistingRecord,
            HttpStatusCode.Conflict);
    }
}
