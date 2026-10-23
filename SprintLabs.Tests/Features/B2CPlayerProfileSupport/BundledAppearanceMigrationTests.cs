using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class BundledAppearanceMigrationTests
{
    [Fact]
    public void Migration_only_adds_nullable_bounded_choices_and_downgrade_only_removes_them()
    {
        var type = typeof(ApplicationDbContext).Assembly.GetTypes().SingleOrDefault(x => x.Name == "AddBundledProfileAppearance");
        type.Should().NotBeNull("existing players must retain their data when the appearance schema is deployed");
        var migration = (Migration)Activator.CreateInstance(type!)!;
        migration.ActiveProvider = "Pomelo.EntityFrameworkCore.MySql";
        migration.UpOperations.Should().HaveCount(2);
        var columns = migration.UpOperations.Should().AllBeOfType<AddColumnOperation>().Subject.Cast<AddColumnOperation>().ToArray();
        columns.Select(x => x.Name).Should().BeEquivalentTo("AvatarAssetPath", "ProfilePictureAssetPath");
        columns.Should().OnlyContain(x => x.Table == "Players" && x.IsNullable && x.MaxLength == 512 && x.DefaultValue == null);
        migration.DownOperations.Should().HaveCount(2).And.AllBeOfType<DropColumnOperation>();
        migration.DownOperations.Cast<DropColumnOperation>().Select(x => x.Name).Should().BeEquivalentTo("AvatarAssetPath", "ProfilePictureAssetPath");
    }
}
