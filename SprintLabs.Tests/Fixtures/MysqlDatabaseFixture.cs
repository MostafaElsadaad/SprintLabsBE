using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Fixtures
{
    public class MysqlDatabaseFixture
    {


        private const string ConnectionString = "Server=localhost;Database=compass-test;User=root;Password=local@db1997;";
        private static readonly object _lock = new();
        private static bool _databaseInitialized;


        public MysqlDatabaseFixture()
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (var context = CreateContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }

                    _databaseInitialized = true;
                }
            }
        }

        public ApplicationDbContext CreateContext()
                => new ApplicationDbContext(
                         new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString)).Options);
    }
}
