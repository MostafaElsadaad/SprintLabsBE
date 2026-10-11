using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Features.QuestionData;

internal sealed class QuestionDataSqliteContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        foreach (var property in builder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
            if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
