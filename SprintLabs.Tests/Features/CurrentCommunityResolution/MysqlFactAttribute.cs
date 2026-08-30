namespace Compass.Tests.Features.CurrentCommunityResolution;

public sealed class MysqlFactAttribute : FactAttribute
{
    public MysqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPRINTLABS_MYSQL_TEST_CONNECTION")))
        {
            Skip = "Set SPRINTLABS_MYSQL_TEST_CONNECTION to run dedicated destructive MySQL concurrency tests.";
        }
    }
}
