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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<QuestionsJson>().HasData(QuestionsJsonSeeder.Seed());
            modelBuilder.Entity<QuestionsJson>(e =>
            {
                e.Property(x => x.Grade).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.PayloadJson).IsRequired();
                e.HasIndex(x => new { x.Grade, x.Assignment });
                e.HasIndex(x => new { x.Grade, x.Assignment, x.Version }).IsUnique();
            });

            // ✅ Player config INSIDE OnModelCreating
            modelBuilder.Entity<Player>(e =>
            {
                e.Property(x => x.GoogleId).IsRequired();
                e.Property(x => x.Email).IsRequired();
                e.Property(x => x.Name).IsRequired();
                e.Property(x => x.Gold).HasDefaultValue(0);
                e.Property(x => x.Experience).HasDefaultValue(0);
                e.Property(x => x.Level).HasDefaultValue(1);
                e.Property(x => x.CreatedAt).IsRequired();
                e.HasIndex(x => x.GoogleId).IsUnique();
            });

            modelBuilder.Entity<User>()
                .Ignore(u => u.PhoneNumber)
                .Ignore(u => u.PhoneNumberConfirmed)
                .Ignore(u => u.TwoFactorEnabled)
                .Ignore(u => u.LockoutEnd)
                .Ignore(u => u.AccessFailedCount)
                .Ignore(u => u.EmailConfirmed)
                .Ignore(u => u.LockoutEnabled);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.GetTableName().Replace("AspNet", ""));
            }
        }
    }
}