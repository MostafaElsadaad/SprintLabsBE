using Infrastructure.Migrations;

using FluentAssertions;

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.CommunityGradesClasses;

public class FixedCommunityGradeMigrationTests
{
    [Fact]
    public void Migration_backfills_recognized_rows_and_fails_closed_for_duplicate_recognition()
    {
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        new ExposedMigration().BuildUp(builder);

        builder.Operations.OfType<AddColumnOperation>()
            .Should().Contain(operation =>
                operation.Table == "Grades" &&
                operation.Name == "Value" &&
                operation.IsNullable);
        builder.Operations.OfType<AddCheckConstraintOperation>()
            .Should().Contain(operation => operation.Name == "CK_Grades_Value_Supported");
        builder.Operations.OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .Should().Contain(sql => sql.Contains("duplicate recognizable grades exist", StringComparison.Ordinal))
            .And.Contain(sql => sql.Contains("INSERT INTO `Grades`", StringComparison.Ordinal))
            .And.Contain(sql => sql.Contains("Value` = `recognized`", StringComparison.Ordinal));
    }

    private sealed class ExposedMigration : FixedCommunityGrades
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }
}
