using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Interfaces;

public interface IJobbaDbContext
{
    public DbSet<JobEntity> Jobs { get; set; }
    public DbSet<JobRegistration> JobRegistrations { get; set; }
    public DbSet<JobProgressEntity> JobProgress { get; set; }
    public Task MigrateAsync(CancellationToken cancellationToken);
}
