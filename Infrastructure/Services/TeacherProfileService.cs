using System.Data;
using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure.DataAccess;
using Infrastructure.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Infrastructure.Services;

public class TeacherProfileService : ITeacherProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _users;

    public TeacherProfileService(ApplicationDbContext context, UserManager<User> users)
    {
        _context = context; _users = users;
    }

    public async Task UpdateAsync(long actorUserId, long communityId, long teacherUserId, string? name, string? email, string? title, CancellationToken ct)
    {
        if ((name == null && email == null && title == null) ||
            (name != null && (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)) ||
            (email != null && (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 256 ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email.Trim()))) ||
            (title != null && title != "TEACHER" && title != "LEAD_TEACHER")) throw Error(HttpStatusCode.BadRequest);
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        await StaffCommunityMembershipIntegrity.LockUserAsync(_context, teacherUserId, ct);
        if (!await _context.CommunityUsers.AnyAsync(x => x.CommunityId == communityId && x.UserId == actorUserId &&
            x.Role == CommunityUserRole.Owner && x.Status == CommunityUserStatus.Active, ct)) throw Error(HttpStatusCode.Forbidden);
        var member = await _context.CommunityUsers.SingleOrDefaultAsync(x => x.CommunityId == communityId &&
            x.UserId == teacherUserId && x.Role == CommunityUserRole.Teacher && x.Status != CommunityUserStatus.Removed, ct)
            ?? throw Error(HttpStatusCode.NotFound);
        var user = await _users.FindByIdAsync(teacherUserId.ToString()) ?? throw Error(HttpStatusCode.NotFound);
        var now = DateTime.UtcNow;
        if (email != null && !string.Equals(user.NormalizedEmail, _users.NormalizeEmail(email.Trim()), StringComparison.Ordinal))
        {
            // Login email remains read-only until a verified email-change workflow is available.
            throw Error(HttpStatusCode.Conflict);
        }
        if (name != null) user.Name = name.Trim();
        user.UpdatedAt = now;
        Ensure(await _users.UpdateAsync(user));
        if (title != null) member.TeacherTitle = title;
        member.UpdatedAt = now;
        _context.StaffActivities.Add(new StaffActivity { CommunityId = communityId, TeacherUserId = teacherUserId,
            ActorUserId = actorUserId, Label = "Updated teacher profile", CreatedAt = now });
        await _context.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw Error(HttpStatusCode.Conflict);
    }

    private static GenericException Error(HttpStatusCode status) => new(ErrorCode.Failure,
        status == HttpStatusCode.NotFound ? ErrorMessage.NotFound : ErrorMessage.InvalidInput, status);
}
