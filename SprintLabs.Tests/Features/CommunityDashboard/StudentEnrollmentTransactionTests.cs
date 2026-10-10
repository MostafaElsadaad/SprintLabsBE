using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Infrastructure.Repositories;
using Application.Features.Communities.StudentLicenses.AddStudentLicense;
using Application.Features.Communities.StudentLicenses.RevokeStudentLicense;
using Application.Features.Communities.StudentLicenses.UpdateStudentLicense;
using Domain.Enums;
using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityDashboard;

public class StudentEnrollmentTransactionTests
{
    [Fact]
    public async Task FailedRow_RollsBackAlreadySavedChangesAndNextRowReadsFreshCapacity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var context = new SqliteStudentContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.Communities.Add(new Community { Id = 1, Name = "School", Slug = "school" });
        context.CommunityLicenses.Add(new CommunityLicense { Id = 1, CommunityId = 1, MaxStudents = 10, UsedStudents = 2 });
        await context.SaveChangesAsync();
        var transactions = new StudentEnrollmentTransaction(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync<int>(1, async () =>
        {
            var capacity = await context.CommunityLicenses.SingleAsync(); capacity.UsedStudents = 8;
            await context.SaveChangesAsync(); throw new InvalidOperationException("Simulated enrollment failure after save.");
        }, default));
        Assert.Equal(2, (await context.CommunityLicenses.SingleAsync()).UsedStudents);
        await context.Database.ExecuteSqlRawAsync("UPDATE CommunityLicenses SET UsedStudents = 3");
        var observed = await transactions.ExecuteAsync(1, async () =>
        {
            var capacity = await context.CommunityLicenses.SingleAsync(); var current = capacity.UsedStudents;
            capacity.UsedStudents++; await context.SaveChangesAsync(); return current;
        }, default);
        Assert.Equal(3, observed);
        Assert.Equal(4, (await context.CommunityLicenses.SingleAsync()).UsedStudents);
    }

    private sealed class SqliteStudentContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    [Fact]
    public async Task ConcurrentEnrollmentAndRevocation_PreserveSeatCounterAcrossRequestContexts()
    {
        var path = Path.Combine(Path.GetTempPath(), "students-concurrency-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=" + path + ";Pooling=False;Default Timeout=10").Options;
        try
        {
            using (var setup = new DashboardFixture(new SqliteStudentContext(options)))
            {
                await setup.Context.Database.EnsureCreatedAsync(); await setup.SeedAsync();
                var license = setup.Context.CommunityLicenses.Single(x => x.CommunityId == 1);
                license.MaxStudents = 3; license.UsedStudents = 2; await setup.Context.SaveChangesAsync();
            }
            var add = Task.Run(async () =>
            {
                using var f = new DashboardFixture(new SqliteStudentContext(options));
                var handler = new AddStudentLicenseCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()),
                    f.Repo<StudentLicense>(), f.Repo<CommunityLicense>(), f.Repo<Grade>(), f.Repo<Domain.Models.Class>(), f.Repo<CommunityUser>(),
                    new PlayerRepository(f.Context), new StudentEnrollmentTransaction(f.Context));
                await handler.Handle(new AddStudentLicenseCommand { UserId = 10, CommunityId = 1, GradeId = 107, ClassId = 1,
                    Email = "concurrent@example.com" }, default);
            });
            var revoke = Task.Run(async () =>
            {
                using var f = new DashboardFixture(new SqliteStudentContext(options));
                var handler = new RevokeStudentLicenseCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()),
                    f.Repo<StudentLicense>(), f.Repo<CommunityLicense>(), f.Repo<CommunityUser>(), new StudentEnrollmentTransaction(f.Context));
                await handler.Handle(new RevokeStudentLicenseCommand { UserId = 10, CommunityId = 1, LicenseId = 2 }, default);
            });
            await Task.WhenAll(add, revoke);
            await using var verify = new SqliteStudentContext(options);
            Assert.Equal(2, (await verify.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents);
            Assert.Equal(2, await verify.StudentLicenses.CountAsync(x => x.CommunityId == 1 && x.Status != Domain.Enums.StudentLicenseStatus.Revoked));
            verify.Matches.Add(new Domain.Models.Match { Id = 1, MatchCode = "test-completed", CommunityId = 1,
                Status = MatchStatus.Completed, CompletedAt = DateTime.UtcNow });
            verify.MatchPlayers.Add(new MatchPlayer { MatchId = 1, PlayerProfileId = 1, CommunityId = 1, CorrectAnswers = 3, WrongAnswers = 1 });
            await verify.SaveChangesAsync();
            using var reads = new DashboardFixture(new SqliteStudentContext(options));
            var roster = await reads.StudentRoster.ReadAsync(20, 1, null, null, "ACTIVE", default);
            Assert.Equal(75m, Assert.Single(roster).AvgScore);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task ConcurrentImportAndPendingEmailUpdate_LeaveOneNonRevokedEnrollmentForTargetEmail()
    {
        var path = Path.Combine(Path.GetTempPath(), "students-email-concurrency-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=" + path + ";Pooling=False;Default Timeout=10").Options;
        try
        {
            using (var setup = new DashboardFixture(new SqliteStudentContext(options)))
            {
                await setup.Context.Database.EnsureCreatedAsync(); await setup.SeedAsync();
                var capacity = setup.Context.CommunityLicenses.Single(x => x.CommunityId == 1);
                capacity.MaxStudents = 10; capacity.UsedStudents = 3; capacity.StudentEmailChangeLimit = 2;
                setup.Context.StudentLicenses.Add(new StudentLicense { Id = 10, CommunityId = 1, ClassId = 1, GradeId = 107,
                    Email = "old@example.com", AssignedByUserId = 10 });
                await setup.Context.SaveChangesAsync();
            }
            var failures = new System.Collections.Concurrent.ConcurrentBag<GenericException>();
            var add = Task.Run(async () =>
            {
                using var f = new DashboardFixture(new SqliteStudentContext(options));
                var handler = new AddStudentLicenseCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()),
                    f.Repo<StudentLicense>(), f.Repo<CommunityLicense>(), f.Repo<Grade>(), f.Repo<Domain.Models.Class>(), f.Repo<CommunityUser>(),
                    new PlayerRepository(f.Context), new StudentEnrollmentTransaction(f.Context));
                try { await handler.Handle(new AddStudentLicenseCommand { UserId = 10, CommunityId = 1, GradeId = 107,
                    ClassId = 1, Email = "target@example.com" }, default); } catch (GenericException ex) { failures.Add(ex); }
            });
            var update = Task.Run(async () =>
            {
                using var f = new DashboardFixture(new SqliteStudentContext(options));
                var handler = new UpdateStudentLicenseCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()),
                    f.Repo<StudentLicense>(), f.Repo<CommunityLicense>(), f.Repo<Grade>(), f.Repo<Domain.Models.Class>(), new StudentEnrollmentTransaction(f.Context));
                try { await handler.Handle(new UpdateStudentLicenseCommand { UserId = 10, CommunityId = 1, GradeId = 107,
                    ClassId = 1, LicenseId = 10, Email = "target@example.com" }, default); } catch (GenericException ex) { failures.Add(ex); }
            });
            await Task.WhenAll(add, update);
            Assert.Single(failures);
            await using var verify = new SqliteStudentContext(options);
            Assert.Equal(1, await verify.StudentLicenses.CountAsync(x => x.CommunityId == 1 && x.Email == "target@example.com" && x.Status != StudentLicenseStatus.Revoked));
            Assert.Equal(await verify.StudentLicenses.CountAsync(x => x.CommunityId == 1 && x.Status != StudentLicenseStatus.Revoked),
                (await verify.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
