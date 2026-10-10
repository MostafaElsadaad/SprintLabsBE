using System.Data;
using Domain.Services;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StudentEnrollmentTransaction(ApplicationDbContext context) : IStudentEnrollmentTransaction
{
    public async Task<T> ExecuteAsync<T>(long communityId, Func<Task<T>> enroll, CancellationToken ct)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
                await context.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM CommunityLicenses WHERE CommunityId = {communityId} FOR UPDATE", ct);
            // Import rows share one request/context. Read current capacity after obtaining the lock,
            // rather than reusing a tracked license from a previous row/transaction.
            context.ChangeTracker.Clear();
            var result = await enroll();
            if (transaction != null) await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync(ct);
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
