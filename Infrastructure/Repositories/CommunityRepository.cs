using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CommunityRepository : ICommunityRepository
{
    private readonly ApplicationDbContext _context;

    public CommunityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        var normalizedSlug = NormalizeSlug(slug);

        return await _context.Communities
            .AnyAsync(x => x.Slug == normalizedSlug);
    }

    public async Task<Community> CreateCommunityAsync(Community community)
    {
        community.Slug = NormalizeSlug(community.Slug);
        community.CreatedAt = DateTime.UtcNow;
        _context.Communities.Add(community);
        await _context.SaveChangesAsync();
        return community;
    }

    public async Task<bool> CommunityExistsAsync(long communityId)
    {
        return await _context.Communities.AnyAsync(x => x.Id == communityId);
    }

    public async Task<List<CommunityListItemProjection>> ListCommunitiesAsync()
    {
        var communities = await _context.Communities
            .Include(x => x.License)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var communityIds = communities.Select(x => x.Id).ToList();

        var owners = await (
                from communityUser in _context.CommunityUsers
                join user in _context.Users on communityUser.UserId equals user.Id
                where communityIds.Contains(communityUser.CommunityId)
                    && communityUser.Role == CommunityUserRole.Owner
                    && communityUser.Status == CommunityUserStatus.Active
                select new CommunityOwnerProjection
                {
                    CommunityUserId = communityUser.Id,
                    CommunityId = communityUser.CommunityId,
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Name = user.Name,
                    Role = communityUser.Role.ToString(),
                    Status = communityUser.Status.ToString()
                })
            .ToListAsync();

        return communities
            .Select(community => new CommunityListItemProjection
            {
                Id = community.Id,
                Name = community.Name,
                Slug = community.Slug,
                Status = community.Status.ToString(),
                Owner = owners.FirstOrDefault(x => x.CommunityId == community.Id),
                License = community.License == null
                    ? null
                    : new CommunityLicenseProjection
                    {
                        Id = community.License.Id,
                        CommunityId = community.License.CommunityId,
                        MaxStudents = community.License.MaxStudents,
                        UsedStudents = community.License.UsedStudents,
                        MaxTeachers = community.License.MaxTeachers,
                        UsedTeachers = community.License.UsedTeachers,
                        StudentEmailChangeLimit = community.License.StudentEmailChangeLimit
                    }
            })
            .ToList();
    }

    public async Task<CommunityOwnerProjection> UpsertOwnerAsync(long communityId, long userId)
    {
        var membership = await _context.CommunityUsers
            .FirstOrDefaultAsync(x => x.CommunityId == communityId && x.UserId == userId);

        if (membership == null)
        {
            membership = new CommunityUser
            {
                CommunityId = communityId,
                UserId = userId,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.CommunityUsers.Add(membership);
        }
        else
        {
            membership.Role = CommunityUserRole.Owner;
            membership.Status = CommunityUserStatus.Active;
            membership.UpdatedAt = DateTime.UtcNow;
            _context.CommunityUsers.Update(membership);
        }

        await _context.SaveChangesAsync();

        var user = await _context.Users.FirstAsync(x => x.Id == userId);

        return new CommunityOwnerProjection
        {
            CommunityUserId = membership.Id,
            CommunityId = membership.CommunityId,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Role = membership.Role.ToString(),
            Status = membership.Status.ToString()
        };
    }

    public async Task<CommunityLicense?> GetLicenseByCommunityIdAsync(long communityId)
    {
        return await _context.CommunityLicenses
            .FirstOrDefaultAsync(x => x.CommunityId == communityId);
    }

    public async Task<CommunityLicense> UpsertLicenseAsync(
        long communityId,
        int maxStudents,
        int maxTeachers,
        int studentEmailChangeLimit)
    {
        var license = await GetLicenseByCommunityIdAsync(communityId);

        if (license == null)
        {
            license = new CommunityLicense
            {
                CommunityId = communityId,
                MaxStudents = maxStudents,
                UsedStudents = 0,
                MaxTeachers = maxTeachers,
                UsedTeachers = 0,
                StudentEmailChangeLimit = studentEmailChangeLimit,
                CreatedAt = DateTime.UtcNow
            };
            _context.CommunityLicenses.Add(license);
        }
        else
        {
            license.MaxStudents = maxStudents;
            license.MaxTeachers = maxTeachers;
            license.StudentEmailChangeLimit = studentEmailChangeLimit;
            license.UpdatedAt = DateTime.UtcNow;
            _context.CommunityLicenses.Update(license);
        }

        await _context.SaveChangesAsync();
        return license;
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }
}
