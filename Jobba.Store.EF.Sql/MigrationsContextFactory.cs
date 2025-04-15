using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Sql.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jobba.Store.EF.Sql;

public class MigrationsContextFactory : IDesignTimeDbContextFactory<JobbaDbContext>
{
    public JobbaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobbaDbContext>();
        optionsBuilder.UsingSqlServer(
            "Data Source=.;Initial Catalog=jobba-sample;Persist Security Info=False;User ID=sa;Password=Password0#@!;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;Max Pool Size=200;");
        return new JobbaDbContext(optionsBuilder.Options);
    }
}
