using System.Threading.Tasks;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Implementations;

public class DefaultJobbaDbInitializer(JobbaDbContext context) : IJobbaDbInitializer
{
    public async Task Initialize() => await context.Database.MigrateAsync();
}
