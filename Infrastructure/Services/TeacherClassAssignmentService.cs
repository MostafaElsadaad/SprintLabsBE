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

public class TeacherClassAssignmentService : ITeacherClassAssignmentService
{
    private readonly ApplicationDbContext _context;

    public TeacherClassAssignmentService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<TeacherClassAssignment>> ReplaceAsync(long communityId, long teacherUserId, IReadOnlyCollection<long> classIds, CancellationToken cancellationToken)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        await StaffCommunityMembershipIntegrity.LockUserAsync(_context, teacherUserId, cancellationToken);

        var teacher = await _context.CommunityUsers.SingleOrDefaultAsync(x => x.CommunityId == communityId && x.UserId == teacherUserId && x.Role == CommunityUserRole.Teacher && x.Status == CommunityUserStatus.Active, cancellationToken);
        if (teacher == null) throw NotFound();

        var ids = classIds.Distinct().ToList();
        var classes = await _context.Classes.Include(x => x.Grade)
            .Where(x => ids.Contains(x.Id) && x.CommunityId == communityId && x.Status == ClassStatus.Active && x.Grade.Value.HasValue && x.Grade.Value >= 7 && x.Grade.Value <= 12)
            .ToListAsync(cancellationToken);
        if (classes.Count != ids.Count) throw NotFound();

        var previous = await _context.TeacherClassAssignments.Where(x => x.TeacherUserId == teacherUserId).ToListAsync(cancellationToken);
        _context.TeacherClassAssignments.RemoveRange(previous);
        var now = DateTime.UtcNow;
        var replacements = classes.Select(x => new TeacherClassAssignment { TeacherUserId = teacherUserId, ClassId = x.Id, Class = x, CreatedAt = now }).ToList();
        _context.TeacherClassAssignments.AddRange(replacements);
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return replacements;
    }

    private static GenericException NotFound() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

    public async Task<long> SaveClassAsync(long actorUserId, long communityId, long? classId, Shared.Requests.DashboardClassRequest request, CancellationToken ct)
    {
        if ((!classId.HasValue && (string.IsNullOrWhiteSpace(request.Name) || !request.GradeId.HasValue)) ||
            (request.Name != null && (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)) ||
            (request.GradeId.HasValue && request.GradeId <= 0) || (request.TeacherId.HasValue && request.TeacherId <= 0) ||
            (classId.HasValue && request.Name == null && request.GradeId == null && request.TeacherId == null)) throw Invalid();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        await EnsureOwnerAsync(actorUserId, communityId, ct);
        var entity = classId.HasValue
            ? await _context.Classes.SingleOrDefaultAsync(x => x.Id == classId && x.CommunityId == communityId && x.Status == ClassStatus.Active, ct) ?? throw NotFound()
            : new Class { CommunityId = communityId, CreatedAt = DateTime.UtcNow };
        var gradeId = request.GradeId ?? entity.GradeId;
        if (!await _context.Grades.AnyAsync(x => x.Id == gradeId && x.CommunityId == communityId && x.Value >= 7 && x.Value <= 12, ct)) throw NotFound();
        var name = request.Name?.Trim() ?? entity.Name;
        if (await _context.Classes.AnyAsync(x => x.CommunityId == communityId && x.GradeId == gradeId && x.Name == name && x.Status == ClassStatus.Active && x.Id != entity.Id, ct)) throw new GenericException(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
        entity.Name = name; entity.GradeId = gradeId; entity.UpdatedAt = DateTime.UtcNow;
        if (!classId.HasValue) _context.Classes.Add(entity);
        // Validate optional teacher before saving the class, including for non-relational test contexts.
        if (request.TeacherId.HasValue) await EnsureTeacherAsync(communityId, request.TeacherId.Value, ct);
        await _context.SaveChangesAsync(ct);
        if (request.TeacherId.HasValue) await AddAssignmentAsync(actorUserId, communityId, entity.Id, request.TeacherId.Value, ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return entity.Id;
    }

    public async Task AssignClassAsync(long actorUserId, long communityId, long classId, long teacherUserId, CancellationToken ct)
    {
        if (classId <= 0 || teacherUserId <= 0) throw Invalid();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        await EnsureOwnerAsync(actorUserId, communityId, ct);
        if (!await _context.Classes.AnyAsync(x => x.Id == classId && x.CommunityId == communityId && x.Status == ClassStatus.Active, ct)) throw NotFound();
        await EnsureTeacherAsync(communityId, teacherUserId, ct);
        await AddAssignmentAsync(actorUserId, communityId, classId, teacherUserId, ct);
        if (transaction != null) await transaction.CommitAsync(ct);
    }

    private async Task EnsureOwnerAsync(long userId, long communityId, CancellationToken ct)
    {
        if (!await _context.CommunityUsers.AnyAsync(x => x.CommunityId == communityId && x.UserId == userId && x.Role == CommunityUserRole.Owner && x.Status == CommunityUserStatus.Active, ct))
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
    }

    private async Task EnsureTeacherAsync(long communityId, long teacherUserId, CancellationToken ct)
    {
        await StaffCommunityMembershipIntegrity.LockUserAsync(_context, teacherUserId, ct);
        if (!await _context.CommunityUsers.AnyAsync(x => x.CommunityId == communityId && x.UserId == teacherUserId && x.Role == CommunityUserRole.Teacher && x.Status == CommunityUserStatus.Active, ct)) throw NotFound();
    }

    private async Task AddAssignmentAsync(long actorUserId, long communityId, long classId, long teacherUserId, CancellationToken ct)
    {
        if (!await _context.TeacherClassAssignments.AnyAsync(x => x.ClassId == classId && x.TeacherUserId == teacherUserId, ct))
        {
            _context.TeacherClassAssignments.Add(new TeacherClassAssignment { ClassId = classId, TeacherUserId = teacherUserId });
            _context.StaffActivities.Add(new StaffActivity { CommunityId = communityId, TeacherUserId = teacherUserId, ActorUserId = actorUserId, Label = "Assigned class" });
            await _context.SaveChangesAsync(ct);
        }
    }

    private static GenericException Invalid() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
}
