using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataMigrationTests
{
    [Fact]
    public void MySql_accepted_answer_key_preserves_accent_distinct_variants()
    {
        var migration = new QuestionDataScheme { ActiveProvider = "Pomelo.EntityFrameworkCore.MySql" };
        var answers = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>(), x => x.Name == "FillBlankAnswers");
        var text = Assert.Single(answers.Columns, x => x.Name == "AcceptedAnswer");
        Assert.Equal("utf8mb4_bin", text.Collation);
    }

    [Fact]
    public void Upgrade_preserves_legacy_packs_and_creates_a_separate_relational_bank()
    {
        var migration = new QuestionDataScheme { ActiveProvider = "Pomelo.EntityFrameworkCore.MySql" };
        Assert.DoesNotContain(migration.UpOperations, x => x is DropColumnOperation or DropTableOperation or DeleteDataOperation);
        Assert.Contains(migration.UpOperations.OfType<RenameTableOperation>(), x => x.Name == "Questions" && x.NewName == "LegacyQuestionPacks");
        var questions = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>(), x => x.Name == "Questions");
        var id = Assert.Single(questions.Columns, x => x.Name == "QuestionId");
        Assert.Equal("binary(16)", id.ColumnType);
        Assert.Equal(new[] { "QuestionId" }, questions.PrimaryKey!.Columns);
        Assert.Equal(15, migration.UpOperations.OfType<CreateTableOperation>().Count());
    }
}
