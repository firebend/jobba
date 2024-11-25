using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Interfaces;

public interface IJobbaDbContext
{
    DbSet<JobEntity> Jobs { get; set; }
    DbSet<JobRegistration> JobRegistrations { get; set; }
    DbSet<JobProgressEntity> JobProgress { get; set; }
    Task MigrateAsync(CancellationToken cancellationToken);
}
