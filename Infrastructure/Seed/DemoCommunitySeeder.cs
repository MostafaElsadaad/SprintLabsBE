using System.Data;

using Domain.Enums;
using Domain.Models;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

public static class DemoCommunitySeeder
{
    private const string Password = "SprintLabsDemo!2026";
    private const string Slug = "sprintlabs-demo-school";
    private const int TeacherCapacity = 10;
    private static readonly (string Email, string Name, bool Teacher)[] Staff =
    [
        ("owner.demo@sprintlabs.local", "Demo Owner", false),
        ("teacher1.demo@sprintlabs.local", "Demo Teacher 1", true),
        ("teacher2.demo@sprintlabs.local", "Demo Teacher 2", true),
        ("teacher3.demo@sprintlabs.local", "Demo Teacher 3", true)
    ];
    private static readonly (int Grade, string Name)[] Classes = [(7, "Class 7A"), (7, "Class 7B"), (8, "Class 8A"), (8, "Class 8B"), (9, "Class 9A"), (10, "Class 10A")];

    public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var community = await context.Communities.SingleOrDefaultAsync(x => x.Slug == Slug, cancellationToken);
        if (community == null)
        {
            community = new Community { Name = "SprintLabs Demo School", Slug = Slug, Status = CommunityStatus.Active };
            context.Communities.Add(community);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (community.Name != "SprintLabs Demo School" || community.Status != CommunityStatus.Active)
        {
            throw new InvalidOperationException("Demo community slug belongs to incompatible data.");
        }

        foreach (var value in Enumerable.Range(7, 6))
        {
            if (!await context.Grades.AnyAsync(x => x.CommunityId == community.Id && x.Value == value, cancellationToken))
                context.Grades.Add(new Grade { CommunityId = community.Id, Value = value, Name = $"Grade {value}", SortOrder = value });
        }
        await context.SaveChangesAsync(cancellationToken);
        var grades = await context.Grades.Where(x => x.CommunityId == community.Id && x.Value.HasValue).ToDictionaryAsync(x => x.Value!.Value, cancellationToken);

        foreach (var item in Classes)
        {
            var existing = await context.Classes.SingleOrDefaultAsync(x => x.CommunityId == community.Id && x.Name == item.Name, cancellationToken);
            if (existing == null) context.Classes.Add(new Class { CommunityId = community.Id, GradeId = grades[item.Grade].Id, Name = item.Name, Status = ClassStatus.Active });
            else if (existing.GradeId != grades[item.Grade].Id || existing.Status != ClassStatus.Active) throw new InvalidOperationException($"Demo class {item.Name} is incompatible.");
        }
        await context.SaveChangesAsync(cancellationToken);
        var classes = await context.Classes.Where(x => x.CommunityId == community.Id).ToDictionaryAsync(x => x.Name, cancellationToken);

        var staffUsers = new Dictionary<string, User>();
        foreach (var staff in Staff)
        {
            var user = await EnsureUser(userManager, staff.Email, staff.Name, staff.Teacher, true);
            staffUsers.Add(staff.Email, user);
            await EnsureMembership(context, community.Id, user.Id, staff.Teacher ? CommunityUserRole.Teacher : CommunityUserRole.Owner, cancellationToken);
        }
        await context.SaveChangesAsync(cancellationToken);

        var assignments = new Dictionary<string, string[]>
        {
            ["teacher1.demo@sprintlabs.local"] = ["Class 7A", "Class 7B"],
            ["teacher2.demo@sprintlabs.local"] = ["Class 8A", "Class 8B"],
            ["teacher3.demo@sprintlabs.local"] = ["Class 7A", "Class 9A", "Class 10A"]
        };
        foreach (var assignment in assignments)
        {
            var teacherId = staffUsers[assignment.Key].Id;
            var wanted = assignment.Value.Select(name => classes[name].Id).ToHashSet();
            var existing = await context.TeacherClassAssignments.Where(x => x.TeacherUserId == teacherId).ToListAsync(cancellationToken);
            context.TeacherClassAssignments.RemoveRange(existing.Where(x => !wanted.Contains(x.ClassId)));
            foreach (var classId in wanted.Where(id => existing.All(x => x.ClassId != id))) context.TeacherClassAssignments.Add(new TeacherClassAssignment { TeacherUserId = teacherId, ClassId = classId });
        }
        await context.SaveChangesAsync(cancellationToken);

        var distribution = new[] { "Class 7A", "Class 7A", "Class 7A", "Class 7A", "Class 7B", "Class 7B", "Class 7B", "Class 7B", "Class 8A", "Class 8A", "Class 8A", "Class 8A", "Class 8B", "Class 8B", "Class 8B", "Class 9A", "Class 9A", "Class 9A", "Class 10A", "Class 10A" };
        for (var index = 0; index < distribution.Length; index++)
        {
            var email = $"student{index + 1:00}.demo@sprintlabs.local";
            var user = await EnsureUser(userManager, email, $"Demo Student {index + 1:00}", false, false);
            var classEntity = classes[distribution[index]];
            var grade = grades.Values.Single(x => x.Id == classEntity.GradeId);
            var player = await context.Players.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (player == null) { player = new Player { UserId = user.Id, Email = email, Name = user.Name, Grade = grade.Value, SchoolName = community.Name, CreatedAt = DateTime.UtcNow }; context.Players.Add(player); await context.SaveChangesAsync(cancellationToken); }
            await EnsureMembership(context, community.Id, user.Id, CommunityUserRole.Student, cancellationToken);
            var studentLicense = await context.StudentLicenses.SingleOrDefaultAsync(x => x.CommunityId == community.Id && x.Email == email, cancellationToken);
            if (studentLicense == null) context.StudentLicenses.Add(new StudentLicense { CommunityId = community.Id, Email = email, UserId = user.Id, PlayerProfileId = player.Id, GradeId = grade.Id, ClassId = classEntity.Id, Status = StudentLicenseStatus.Active, AssignedByUserId = staffUsers["owner.demo@sprintlabs.local"].Id, ActivatedAt = DateTime.UtcNow });
            else if (studentLicense.UserId != user.Id || studentLicense.PlayerProfileId != player.Id || studentLicense.GradeId != grade.Id || studentLicense.ClassId != classEntity.Id) throw new InvalidOperationException($"Demo student {email} is incompatible.");
        }
        await context.SaveChangesAsync(cancellationToken);
        var license = await context.CommunityLicenses.SingleOrDefaultAsync(x => x.CommunityId == community.Id, cancellationToken);
        if (license == null) { license = new CommunityLicense { CommunityId = community.Id, StudentEmailChangeLimit = 2 }; context.CommunityLicenses.Add(license); }
        license.UsedStudents = await context.StudentLicenses.CountAsync(x => x.CommunityId == community.Id && x.Status != StudentLicenseStatus.Revoked, cancellationToken);
        license.UsedTeachers = await context.CommunityUsers.CountAsync(x => x.CommunityId == community.Id && x.Role == CommunityUserRole.Teacher && (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending), cancellationToken);
        license.MaxStudents = Math.Max(license.MaxStudents, license.UsedStudents);
        license.MaxTeachers = Math.Max(license.MaxTeachers, Math.Max(license.UsedTeachers, TeacherCapacity));
        await context.SaveChangesAsync(cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<User> EnsureUser(UserManager<User> manager, string email, string name, bool teacher, bool password)
    {
        var user = await manager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User { UserName = email, Email = email, Name = name, EmailConfirmed = true, IsTeacherAccount = teacher, Status = UserStatus.Active, LockoutEnabled = true };
            var result = password ? await manager.CreateAsync(user, Password) : await manager.CreateAsync(user);
            if (!result.Succeeded) throw new InvalidOperationException($"Demo identity {email} could not be created: {string.Join(" ", result.Errors.Select(x => x.Description))}");
        }
        else if (user.IsTeacherAccount != teacher || user.Status != UserStatus.Active || !user.EmailConfirmed || (password && !await manager.CheckPasswordAsync(user, Password))) throw new InvalidOperationException($"Demo identity {email} is incompatible.");
        return user;
    }

    private static async Task EnsureMembership(ApplicationDbContext context, long communityId, long userId, CommunityUserRole role, CancellationToken cancellationToken)
    {
        if ((role == CommunityUserRole.Owner || role == CommunityUserRole.Teacher) &&
            await context.CommunityUsers.AnyAsync(x =>
                x.UserId == userId &&
                x.CommunityId != communityId &&
                (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending),
                cancellationToken))
        {
            throw new InvalidOperationException("Demo staff identity already has a current staff membership in another community.");
        }

        var membership = await context.CommunityUsers.SingleOrDefaultAsync(x => x.CommunityId == communityId && x.UserId == userId, cancellationToken);
        if (membership == null) context.CommunityUsers.Add(new CommunityUser { CommunityId = communityId, UserId = userId, Role = role, Status = CommunityUserStatus.Active });
        else if (membership.Role != role && membership.Status != CommunityUserStatus.Removed) throw new InvalidOperationException("Demo membership is incompatible.");
        else { membership.Role = role; membership.Status = CommunityUserStatus.Active; membership.UpdatedAt = DateTime.UtcNow; }
    }
}
