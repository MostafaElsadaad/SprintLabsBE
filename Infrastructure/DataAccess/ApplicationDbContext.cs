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
                    .IsRequired()
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
                e.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<User>(e =>
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

                e.Property(x => x.IsPlatformAdmin)
                    .HasDefaultValue(false);

                e.Property(x => x.Status)
                    .IsRequired()
                    .HasDefaultValue(UserStatus.Active);

                e.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.GoogleId);

                e.HasOne(x => x.Player)
                    .WithOne()
                    .HasForeignKey<Player>(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.Ignore(u => u.PhoneNumber)
                    .Ignore(u => u.PhoneNumberConfirmed)
                    .Ignore(u => u.TwoFactorEnabled)
                    .Ignore(u => u.LockoutEnd)
                    .Ignore(u => u.AccessFailedCount)
                    .Ignore(u => u.EmailConfirmed)
                    .Ignore(u => u.LockoutEnabled);
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
