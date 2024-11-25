using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jobba.Store.EF.Sqlite;

public class MigrationsContextFactory : IDesignTimeDbContextFactory<JobbaDbContext>
{
    public JobbaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobbaDbContext>();
        optionsBuilder.UsingSqlite(
            "DataSource=:memory:");
        return new JobbaDbContext(optionsBuilder.Options);
    }
}
