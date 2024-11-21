using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.SqlMigrations.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jobba.Store.EF.SqlMigrations;

public class MigrationsContextFactory : IDesignTimeDbContextFactory<JobbaDbContext>
{
    public JobbaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobbaDbContext>();
        // TODO - Update connection string
        optionsBuilder.UsingSqlServer(
            "Data Source=.;Initial Catalog=jobba-sample;Persist Security Info=False;User ID=sa;Password=Password0#@!;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;Max Pool Size=200;");
        return new JobbaDbContext(optionsBuilder.Options);
    }
}
