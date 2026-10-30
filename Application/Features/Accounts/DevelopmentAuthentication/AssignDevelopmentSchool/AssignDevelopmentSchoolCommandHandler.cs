using System.Data.Common;
using System.Net;
using Application.Features.Communities.StudentLicenses.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.DevelopmentAuthentication;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;

public sealed class AssignDevelopmentSchoolCommandHandler(
    IDevelopmentAuthenticationGuard guard, IUserService users, IPlayerRepository players,
    IBaseRepository<Community> communities, IBaseRepository<CommunityLicense> capacity,
    IBaseRepository<StudentLicense> licenses, IBaseRepository<CommunityUser> memberships,
    IBaseRepository<Grade> grades, IBaseRepository<Class> classes, IStudentEnrollmentTransaction transaction)
    : IRequestHandler<AssignDevelopmentSchoolCommand, AssignDevelopmentSchoolResponse>
{
    public async Task<AssignDevelopmentSchoolResponse> Handle(AssignDevelopmentSchoolCommand request, CancellationToken ct)
    {
        // Conceal unavailable development functionality before any account lookup.
        guard.EnsureEndpointAccess(request.SuppliedApiKeys);
        var admin = await users.GetCurrentUser(request.AdminUserId);
        if (admin == null || !admin.IsPlatformAdmin || admin.IsSuspended || admin.IsLockedOut ||
            !string.Equals(admin.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw Error(HttpStatusCode.Forbidden, ErrorMessage.InvalidAccessToken);
        if (request.CommunityId <= 0 || request.GradeId <= 0 || request.ClassId <= 0)
            throw Error(HttpStatusCode.BadRequest, ErrorMessage.InvalidInput);
        if (!DevelopmentPlayerCatalog.TryGet(request.AccountKey, out var definition))
            throw Error(HttpStatusCode.NotFound, ErrorMessage.NotFound);
        var user = await users.FindByEmail(definition.Email);
        if (user == null) throw Error(HttpStatusCode.NotFound, ErrorMessage.NotFound);
        if (user.IsSuspended || user.IsLockedOut || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw Error(HttpStatusCode.Forbidden, ErrorMessage.InvalidAccessToken);
        var player = await players.GetByUserIdAsync(user.Id);
        // Only the persisted catalogue identity may be assigned. Never create/repair identities or
        // modify Google/Firebase fields to make an ordinary player resemble a development account.
        if (user.IsPlatformAdmin || user.IsTeacherAccount || !string.IsNullOrWhiteSpace(user.GoogleId) ||
            !string.IsNullOrWhiteSpace(user.FirebaseUid) || user.UserName != definition.AccountKey ||
            !string.Equals(user.Email, definition.Email, StringComparison.OrdinalIgnoreCase) || player == null ||
            user.PlayerProfileId != player.Id || player.UserId != user.Id || !string.IsNullOrWhiteSpace(player.GoogleId) ||
            !string.Equals(player.Email, definition.Email, StringComparison.OrdinalIgnoreCase))
            throw Conflict();
        try
        {
            return await transaction.ExecuteAsync(request.CommunityId,
                () => Assign(request, user.Id, player.Id, definition.Email, ct), ct);
        }
        catch (DbUpdateException) { throw Unavailable(); }
        catch (DbException) { throw Unavailable(); }
    }

    private async Task<AssignDevelopmentSchoolResponse> Assign(AssignDevelopmentSchoolCommand request,
        long userId, long profileId, string email, CancellationToken ct)
    {
        if (!await communities.AsQueryable().AnyAsync(c => c.Id == request.CommunityId && c.Status == CommunityStatus.Active, ct))
            throw Error(HttpStatusCode.NotFound, ErrorMessage.NotFound);
        await StudentLicenseValidation.EnsureValidGradeClass(grades, classes, request.CommunityId, request.GradeId, request.ClassId, ct);
        var membership = await memberships.AsQueryable().SingleOrDefaultAsync(m => m.CommunityId == request.CommunityId && m.UserId == userId, ct);
        if (membership != null && membership.Role != CommunityUserRole.Student) throw Conflict();
        var matches = await licenses.AsQueryable().Where(l => l.CommunityId == request.CommunityId &&
            l.Status != StudentLicenseStatus.Revoked && (l.Email.ToLower() == email || l.UserId == userId || l.PlayerProfileId == profileId)).ToListAsync(ct);
        if (matches.Count > 1) throw Conflict();
        var license = matches.SingleOrDefault();
        if (license != null && (!string.Equals(license.Email, email, StringComparison.OrdinalIgnoreCase) ||
            license.GradeId != request.GradeId || license.ClassId != request.ClassId ||
            (license.UserId.HasValue && license.UserId != userId) ||
            (license.PlayerProfileId.HasValue && license.PlayerProfileId != profileId))) throw Conflict();
        if (license?.Status == StudentLicenseStatus.Active && (license.UserId != userId || license.PlayerProfileId != profileId)) throw Conflict();
        bool alreadyAssigned = license?.Status == StudentLicenseStatus.Active && membership?.Status == CommunityUserStatus.Active;
        if (alreadyAssigned) return Response(request, profileId, license!, true);

        var seats = await capacity.AsQueryable().SingleOrDefaultAsync(c => c.CommunityId == request.CommunityId, ct);
        if (seats == null || seats.UsedStudents < 0) throw Error(HttpStatusCode.BadRequest, ErrorMessage.NoStudentLicenseSeats);
        var now = DateTime.UtcNow;
        if (license == null)
        {
            if (seats.UsedStudents >= seats.MaxStudents) throw Error(HttpStatusCode.BadRequest, ErrorMessage.NoStudentLicenseSeats);
            license = new StudentLicense { CommunityId = request.CommunityId, Email = email,
                GradeId = request.GradeId, ClassId = request.ClassId, AssignedByUserId = request.AdminUserId, CreatedAt = now };
            await licenses.AddAsync(license);
            seats.UsedStudents++; seats.UpdatedAt = now;
            await capacity.UpdateAsync(seats);
        }
        else if (seats.UsedStudents == 0) throw Conflict(); // Do not silently repair inconsistent seat accounting.
        license.UserId = userId; license.PlayerProfileId = profileId;
        license.Status = StudentLicenseStatus.Active; license.ActivatedAt ??= now; license.UpdatedAt = now;
        if (membership == null)
            await memberships.AddAsync(new CommunityUser { CommunityId = request.CommunityId, UserId = userId,
                Role = CommunityUserRole.Student, Status = CommunityUserStatus.Active, CreatedAt = now });
        else { membership.Status = CommunityUserStatus.Active; membership.UpdatedAt = now; await memberships.UpdateAsync(membership); }
        // All repositories share the same scoped context and the existing community enrollment transaction.
        await licenses.SaveChangesAsync();
        return Response(request, profileId, license, false);
    }

    private static AssignDevelopmentSchoolResponse Response(AssignDevelopmentSchoolCommand request, long profileId, StudentLicense license, bool replay) => new()
    {
        AccountKey = request.AccountKey, PlayerProfileId = profileId, CommunityId = request.CommunityId,
        GradeId = request.GradeId, ClassId = request.ClassId, StudentLicenseId = license.Id, AlreadyAssigned = replay
    };
    private static GenericException Error(HttpStatusCode code, string message) => new(ErrorCode.Failure, message, code);
    private static GenericException Conflict() => Error(HttpStatusCode.Conflict, "Development school assignment conflicts with existing identity or enrollment.");
    private static GenericException Unavailable() => Error(HttpStatusCode.ServiceUnavailable, "Development assignment is temporarily unavailable. Retry the same assignment.");
}
