using Domain.Enums;
using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;

namespace Compass.Tests.Features.MatchProgression;

internal sealed class ProgressionFixture : IDisposable
{
    public ApplicationDbContext Context { get; }
    public MatchProgressionService Matches { get; }
    public ProgressionReadService Reads { get; }
    public ProgressionFixture(ApplicationDbContext? context = null)
    {
        Context = context ?? new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Matches = new(Context, new Infrastructure.Services.XpCalculationService(), new Infrastructure.Services.LevelProgressionService(), new Infrastructure.Services.RpRankCalculationService());
        Reads = new(Context, new Infrastructure.Services.LevelProgressionService(), new CommunityAccessService(new BaseRepository<CommunityUser>(Context)));
    }

    public async Task SeedAsync()
    {
        foreach (var id in new long[] { 1, 2, 3, 10, 20, 30, 40 })
            Context.Users.Add(new User { Id = id, Name = $"User {id}", UserName = $"u{id}@example.com",
                Email = $"u{id}@example.com", NormalizedEmail = $"U{id}@EXAMPLE.COM", IsPlatformAdmin = id == 40 });
        Context.Players.AddRange(new Player { Id = 101, UserId = 1, Name = "One", Email = "u1@example.com", Rp = 450, RankTier = RankTier.Seeker },
            new Player { Id = 102, UserId = 2, Name = "Two", Email = "u2@example.com", Rp = 1000, RankTier = RankTier.Arcanist },
            new Player { Id = 103, UserId = 3, Name = "Three", Email = "u3@example.com", Rp = 1000, RankTier = RankTier.Arcanist });
        Context.Communities.AddRange(new Community { Id = 1, Name = "School", Slug = "school" },
            new Community { Id = 2, Name = "Other", Slug = "other" });
        Context.CommunityUsers.AddRange(new CommunityUser { CommunityId = 1, UserId = 10, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active },
            new CommunityUser { CommunityId = 1, UserId = 20, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Active },
            new CommunityUser { CommunityId = 2, UserId = 30, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        Context.Grades.AddRange(new Grade { Id = 1, CommunityId = 1, Name = "Seven", Value = 7 },
            new Grade { Id = 2, CommunityId = 2, Name = "Seven", Value = 7 });
        Context.Classes.AddRange(new Class { Id = 1, CommunityId = 1, GradeId = 1, Name = "A" },
            new Class { Id = 2, CommunityId = 1, GradeId = 1, Name = "B" },
            new Class { Id = 3, CommunityId = 2, GradeId = 2, Name = "C" });
        Context.TeacherClassAssignments.Add(new TeacherClassAssignment { ClassId = 1, TeacherUserId = 20 });
        foreach (var id in new long[] { 101, 102, 103 })
            Context.StudentLicenses.Add(new StudentLicense { CommunityId = id == 103 ? 2 : 1,
                GradeId = id == 103 ? 2 : 1, ClassId = id == 101 ? 1 : id == 102 ? 2 : 3,
                PlayerProfileId = id, UserId = id - 100, Email = $"u{id - 100}@example.com",
                Status = StudentLicenseStatus.Active, AssignedByUserId = 10 });
        await Context.SaveChangesAsync();
    }

    public RegisterMatchRequest Registration(string type = "Ranked", long? communityId = null, int totalPlayers = 2) => new()
    { MatchCode = Guid.NewGuid().ToString(), MatchType = type, CommunityId = communityId,
        MirrorRoomId = "mirror-room", StartedAt = DateTime.UtcNow.AddMinutes(-10), TotalPlayers = totalPlayers,
        PlayerProfileIds = new() { 101, 102 } };

    public CompleteMatchRequest Completion(string type = "Ranked", long? communityId = null) => new()
    {
        MatchType = type, CommunityId = communityId, MirrorRoomId = "mirror-room", EndedAt = DateTime.UtcNow,
        Players = new()
        {
            new() { PlayerProfileId = 101, Position = 1, CorrectAnswers = 3, WrongAnswers = 1, MaxStreak = 2, AnswerTimeTotalMs = 4000, QuestionTimeTotalMs = 40000 },
            new() { PlayerProfileId = 102, Position = 2 }
        },
        QuestionResults = new()
        {
            new() { PlayerProfileId = 101, QuestionType = "MCQ", IsCorrect = true, AnswerTimeMs = 1000, QuestionTimeMs = 10000, StreakBeforeAnswer = 0, StreakAfterAnswer = 1 },
            new() { PlayerProfileId = 101, QuestionType = "MCQ", IsCorrect = true, AnswerTimeMs = 1000, QuestionTimeMs = 10000, StreakBeforeAnswer = 1, StreakAfterAnswer = 2 },
            new() { PlayerProfileId = 101, QuestionType = "MCQ", IsCorrect = false, AnswerTimeMs = 1000, QuestionTimeMs = 10000, StreakBeforeAnswer = 2, StreakAfterAnswer = 0 },
            new() { PlayerProfileId = 101, QuestionType = "MCQ", IsCorrect = true, AnswerTimeMs = 1000, QuestionTimeMs = 10000, StreakBeforeAnswer = 0, StreakAfterAnswer = 1 }
        }
    };
    public void Dispose() => Context.Dispose();
}
