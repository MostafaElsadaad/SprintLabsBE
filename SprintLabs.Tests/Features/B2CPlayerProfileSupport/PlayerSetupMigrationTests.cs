using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class PlayerSetupMigrationTests
{
    [Fact]
    public void Setup_migration_preserves_existing_players_and_seeds_three_curriculum_language_pairs()
    {
        var type = typeof(ApplicationDbContext).Assembly.GetTypes().Single(x => x.Name == "AddB2CPlayerSetup");
        var migration = (Migration)Activator.CreateInstance(type)!;
        migration.ActiveProvider = "Pomelo.EntityFrameworkCore.MySql";
        migration.UpOperations.Should().HaveCount(4);
        var columns = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        columns.Select(x => x.Name).Should().BeEquivalentTo("Curriculum", "Language");
        columns.Should().OnlyContain(x => x.Table == "Players" && x.IsNullable && x.DefaultValue == null);
        migration.UpOperations.OfType<CreateTableOperation>().Single().Name.Should().Be("PlayerEducationOptions");
        var seeds = migration.UpOperations.OfType<InsertDataOperation>().Single();
        seeds.Table.Should().Be("PlayerEducationOptions");
        seeds.Values.GetLength(0).Should().Be(3);
        migration.UpOperations.Should().NotContain(x => x is UpdateDataOperation || x is DeleteDataOperation);
    }
}
