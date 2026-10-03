namespace Domain.Services;

public interface ITeacherProfileService
{
    Task UpdateAsync(long actorUserId, long communityId, long teacherUserId, string? name, string? email, string? title, CancellationToken cancellationToken);
}
