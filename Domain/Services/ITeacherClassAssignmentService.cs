using Domain.Models;

namespace Domain.Services;

public interface ITeacherClassAssignmentService
{
    Task<IReadOnlyList<TeacherClassAssignment>> ReplaceAsync(long communityId, long teacherUserId, IReadOnlyCollection<long> classIds, CancellationToken cancellationToken);
}
