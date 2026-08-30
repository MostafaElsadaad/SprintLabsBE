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
}
