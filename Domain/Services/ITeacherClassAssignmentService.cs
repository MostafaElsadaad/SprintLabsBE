using Domain.Models;

namespace Domain.Services;

public interface ITeacherClassAssignmentService
{
    Task<IReadOnlyList<TeacherClassAssignment>> ReplaceAsync(long communityId, long teacherUserId, IReadOnlyCollection<long> classIds, CancellationToken cancellationToken);
    Task<long> SaveClassAsync(long actorUserId, long communityId, long? classId, Shared.Requests.DashboardClassRequest request, CancellationToken cancellationToken);
    Task AssignClassAsync(long actorUserId, long communityId, long classId, long teacherUserId, CancellationToken cancellationToken);
}
