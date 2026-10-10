namespace Domain.Services;

public interface IStudentEnrollmentTransaction
{
    Task<T> ExecuteAsync<T>(long communityId, Func<Task<T>> enroll, CancellationToken ct);
}
