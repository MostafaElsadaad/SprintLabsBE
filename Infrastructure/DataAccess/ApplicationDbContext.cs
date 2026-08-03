using Domain.Enums;
using Domain.Models;

using Infrastructure.Seed;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<QuestionsJson> Questions { get; set; }
        public DbSet<Player> Players { get; set; }   // ✅ INSIDE the class
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityUser> CommunityUsers { get; set; }
        public DbSet<CommunityLicense> CommunityLicenses { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<StudentLicense> StudentLicenses { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchPlayer> MatchPlayers { get; set; }
        public DbSet<MatchQuestionResult> MatchQuestionResults { get; set; }
        public DbSet<MatchRewardResult> MatchRewardResults { get; set; }
        public DbSet<PlayerXpLog> PlayerXpLogs { get; set; }
        public DbSet<PlayerRankLog> PlayerRankLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<TeacherInvitation> TeacherInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<QuestionsJson>().HasData(QuestionsJsonSeeder.Seed());
            modelBuilder.Entity<QuestionsJson>(e =>
            {
                e.Property(x => x.Grade).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.PayloadJson).IsRequired();

                e.HasIndex(x => x.Grade).IsUnique();

                e.HasIndex(x => new { x.Grade, x.Assignment });
            });

            // ✅ Player config INSIDE OnModelCreating
            modelBuilder.Entity<Player>(e =>
            {
                e.Property(x => x.GoogleId)
                    .HasMaxLength(128);

                e.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(x => x.AvatarUrl)
                    .HasMaxLength(500);

                e.Property(x => x.Grade)
                    .HasMaxLength(20);

                e.Property(x => x.SchoolName)
                    .HasMaxLength(200);

                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.GoogleId).IsUnique();
                e.HasIndex(x => x.UserId).IsUnique();

                e.Property(x => x.Gold).HasDefaultValue(0);
                e.Property(x => x.Experience).HasDefaultValue(0);
                e.Property(x => x.Level).HasDefaultValue(1);
                e.Property(x => x.Rp).HasDefaultValue(0);
                e.Property(x => x.RankTier).HasDefaultValue(RankTier.Student);
                e.Property(x => x.HighestRankTier).HasDefaultValue(RankTier.Student);
                e.Property(x => x.TotalMatches).HasDefaultValue(0);
                e.Property(x => x.TotalWins).HasDefaultValue(0);
                e.Property(x => x.CreatedAt).IsRequired();

                e.HasIndex(x => x.Rp);
                e.HasIndex(x => x.RankTier);
                e.HasIndex(x => x.Level);
                e.HasIndex(x => x.Experience);
                e.HasIndex(x => x.TotalWins);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.Property(x => x.GoogleId)
                    .HasMaxLength(128);

                e.Property(x => x.FirebaseUid)
                    .HasMaxLength(128);

                e.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(x => x.AvatarUrl)
                    .HasMaxLength(500);

                e.Property(x => x.IsPlatformAdmin)
                    .HasDefaultValue(false);

                e.Property(x => x.IsTeacherAccount)
                    .HasDefaultValue(false);

                e.Property(x => x.LastConfirmationEmailSentAt);
                e.Property(x => x.LastPasswordResetEmailSentAt);

                e.Property(x => x.EmailConfirmed)
                    .HasDefaultValue(false);

                e.Property(x => x.LockoutEnabled)
                    .HasDefaultValue(true);

                e.Property(x => x.AccessFailedCount)
                    .HasDefaultValue(0);

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(UserStatus.Active);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.NormalizedEmail).IsUnique();
                e.HasIndex(x => x.GoogleId);
                e.HasIndex(x => x.FirebaseUid).IsUnique();

                e.HasOne(x => x.Player)
                    .WithOne()
                    .HasForeignKey<Player>(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.Ignore(u => u.PhoneNumber)
                    .Ignore(u => u.PhoneNumberConfirmed)
                    .Ignore(u => u.TwoFactorEnabled);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                e.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
                e.Property(x => x.CreatedByIp).HasMaxLength(64);
                e.Property(x => x.RevokedByIp).HasMaxLength(64);
                e.Property(x => x.ExpiresAt).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => new { x.UserId, x.RevokedAt, x.ExpiresAt });
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TeacherInvitation>(e =>
            {
                e.Property(x => x.InvitedEmail).IsRequired().HasMaxLength(256);
                e.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                e.Property(x => x.ExpiresAt).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => x.CommunityUserId);
                e.HasIndex(x => new { x.CommunityUserId, x.RevokedAt, x.ExpiresAt });
                e.HasIndex(x => new { x.CommunityUserId, x.AcceptedAt, x.RevokedAt, x.ExpiresAt });
                e.HasOne(x => x.CommunityUser)
                    .WithMany()
                    .HasForeignKey(x => x.CommunityUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Community>(e =>
            {
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                e.Property(x => x.Slug)
                    .IsRequired()
                    .HasMaxLength(120);

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(CommunityStatus.Active);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.Slug).IsUnique();

                e.HasMany(x => x.CommunityUsers)
                    .WithOne(x => x.Community)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Grades)
                    .WithOne(x => x.Community)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Classes)
                    .WithOne(x => x.Community)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.StudentLicenses)
                    .WithOne(x => x.Community)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.License)
                    .WithOne(x => x.Community)
                    .HasForeignKey<CommunityLicense>(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CommunityUser>(e =>
            {
                e.Property(x => x.Role)
                    .IsRequired();

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(CommunityUserStatus.Active);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => new { x.CommunityId, x.UserId }).IsUnique();
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => new { x.UserId, x.Role, x.Status, x.CommunityId });

                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommunityLicense>(e =>
            {
                e.Property(x => x.MaxStudents).HasDefaultValue(0);
                e.Property(x => x.UsedStudents).HasDefaultValue(0);
                e.Property(x => x.MaxTeachers).HasDefaultValue(0);
                e.Property(x => x.UsedTeachers).HasDefaultValue(0);
                e.Property(x => x.StudentEmailChangeLimit).HasDefaultValue(0);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.CommunityId).IsUnique();
            });

            modelBuilder.Entity<Grade>(e =>
            {
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(120);

                e.Property(x => x.SortOrder)
                    .IsRequired();

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => new { x.CommunityId, x.SortOrder });
            });

            modelBuilder.Entity<Class>(e =>
            {
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(120);

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(ClassStatus.Active);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => x.GradeId);
                e.HasIndex(x => new { x.CommunityId, x.Status });

                e.HasOne(x => x.Grade)
                    .WithMany(x => x.Classes)
                    .HasForeignKey(x => x.GradeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentLicense>(e =>
            {
                e.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(StudentLicenseStatus.Pending);

                e.Property(x => x.EmailChangeCount)
                    .HasDefaultValue(0);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => new { x.CommunityId, x.Email, x.Status });
                e.HasIndex(x => new { x.CommunityId, x.Status });
                e.HasIndex(x => new { x.CommunityId, x.GradeId });
                e.HasIndex(x => new { x.CommunityId, x.ClassId });
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.PlayerProfileId);

                e.HasOne(x => x.Grade)
                    .WithMany()
                    .HasForeignKey(x => x.GradeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Class)
                    .WithMany()
                    .HasForeignKey(x => x.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne<Player>()
                    .WithMany()
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Match>(e =>
            {
                e.Property(x => x.MatchCode)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.MirrorRoomId)
                    .HasMaxLength(128);

                e.Property(x => x.MatchType)
                    .IsRequired();

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(MatchStatus.Created);

                e.Property(x => x.StartedAt)
                    .IsRequired();

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.MatchCode);
                e.HasIndex(x => x.MirrorRoomId);
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CompletedAt);

                e.HasOne(x => x.Community)
                    .WithMany(x => x.Matches)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MatchPlayer>(e =>
            {
                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.MatchId);
                e.HasIndex(x => x.PlayerProfileId);
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => new { x.MatchId, x.PlayerProfileId }).IsUnique();

                e.HasOne(x => x.Match)
                    .WithMany(x => x.MatchPlayers)
                    .HasForeignKey(x => x.MatchId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.PlayerProfile)
                    .WithMany(x => x.MatchPlayers)
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Community)
                    .WithMany(x => x.MatchPlayers)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MatchQuestionResult>(e =>
            {
                e.Property(x => x.QuestionType)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.MatchId);
                e.HasIndex(x => x.PlayerProfileId);
                e.HasIndex(x => x.QuestionId);

                e.HasOne(x => x.Match)
                    .WithMany(x => x.MatchQuestionResults)
                    .HasForeignKey(x => x.MatchId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.PlayerProfile)
                    .WithMany(x => x.MatchQuestionResults)
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MatchRewardResult>(e =>
            {
                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.MatchId);
                e.HasIndex(x => x.PlayerProfileId);
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => new { x.MatchId, x.PlayerProfileId }).IsUnique();

                e.HasOne(x => x.Match)
                    .WithMany(x => x.MatchRewardResults)
                    .HasForeignKey(x => x.MatchId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.PlayerProfile)
                    .WithMany(x => x.MatchRewardResults)
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Community)
                    .WithMany(x => x.MatchRewardResults)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PlayerXpLog>(e =>
            {
                e.Property(x => x.SourceType)
                    .IsRequired();

                e.Property(x => x.Multiplier)
                    .HasPrecision(10, 4);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.PlayerProfileId);
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => x.SourceType);
                e.HasIndex(x => x.CreatedAt);

                e.HasOne(x => x.PlayerProfile)
                    .WithMany(x => x.PlayerXpLogs)
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Community)
                    .WithMany(x => x.PlayerXpLogs)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PlayerRankLog>(e =>
            {
                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.PlayerProfileId);
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => x.MatchId);
                e.HasIndex(x => x.CreatedAt);

                e.HasOne(x => x.PlayerProfile)
                    .WithMany(x => x.PlayerRankLogs)
                    .HasForeignKey(x => x.PlayerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Community)
                    .WithMany(x => x.PlayerRankLogs)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Match)
                    .WithMany()
                    .HasForeignKey(x => x.MatchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName != null)
                {
                    entityType.SetTableName(tableName.Replace("AspNet", ""));
                }
            }
        }
    }
}