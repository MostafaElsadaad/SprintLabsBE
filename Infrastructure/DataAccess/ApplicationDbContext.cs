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

        //public DbSet<Squad> Squads { get; set; }
        //public DbSet<Tribe> Tribes { get; set; }
        //public DbSet<Client> Clients { get; set; }
        //public DbSet<Content> Contents { get; set; }
        //public DbSet<Project> Projects { get; set; }
        //public DbSet<UserProfile> UserProfiles { get; set; }
        //public DbSet<ProjectMember> ProjectMembers { get; set; }
        //public DbSet<Environment> Environments { get; set; }
        //public DbSet<Link> Links { get; set; }
        //public DbSet<Api> Apis { get; set; }
        public DbSet<QuestionsJson> Questions { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seeds
            //modelBuilder.Entity<Tribe>().HasData(TribeSeeder.Seed());
            //modelBuilder.Entity<Squad>().HasData(SquadSeeder.Seed());
            //modelBuilder.Entity<Client>().HasData(ClientSeeder.Seed());
            //modelBuilder.Entity<Project>().HasData(ProjectSeeder.Seed());
            //modelBuilder.Entity<Content>().HasData(ContentSeeder.Seed());
            //modelBuilder.Entity<Environment>().HasData(EnvironmentSeeder.Seed());
            //modelBuilder.Entity<Link>().HasData(LinkSeeder.Seed());
            //modelBuilder.Entity<UserProfile>().HasData(UserProfileSeeder.Seed());
            //modelBuilder.Entity<Api>().HasData(ApiSeeder.Seed());
            modelBuilder.Entity<QuestionsJson>().HasData(QuestionsJsonSeeder.Seed());


            modelBuilder.Entity<QuestionsJson>(e =>
            {
                // Required fields
                e.Property(x => x.Grade).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();

                // If using string JSON
                 e.Property(x => x.PayloadJson).IsRequired();

                // Main lookup index
                e.HasIndex(x => new { x.Grade, x.Assignment });

                // If you want ONE row per (Grade, Assignment, Version)
                e.HasIndex(x => new { x.Grade, x.Assignment, x.Version }).IsUnique();

                // If you want ONE row per (Grade, Assignment) only (no versions):
                 //e.HasIndex(x => new { x.Grade, x.Assignment }).IsUnique();
            });


            modelBuilder.Entity<User>()
                .Ignore(u => u.PhoneNumber)
                .Ignore(u => u.PhoneNumberConfirmed)
                .Ignore(u => u.TwoFactorEnabled)
                .Ignore(u => u.LockoutEnd)
                .Ignore(u => u.AccessFailedCount)
                .Ignore(u => u.EmailConfirmed)
                .Ignore(u => u.LockoutEnabled);



            //remove the aspNet prefix from the table names
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.GetTableName().Replace("AspNet", ""));
            }
        }
    }
}