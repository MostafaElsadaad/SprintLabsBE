using Infrastructure.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataSchemaTests
{
    [Fact]
    public async Task Questions_store_shared_uuid_key_instead_of_question_pack_payload()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new QuestionDataSqliteContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Questions')";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new Dictionary<string, long>();
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1), reader.GetInt64(5));
        Assert.Contains("QuestionId", columns.Keys);
        Assert.Equal(1, columns["QuestionId"]);
        Assert.DoesNotContain("PayloadJson", columns.Keys);
        Assert.Contains("Curriculum", columns.Keys);
        Assert.Contains("Language", columns.Keys);
    }
}
