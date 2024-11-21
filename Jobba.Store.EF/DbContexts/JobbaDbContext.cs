using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Jobba.Store.EF.DbContexts;

public class JobbaDbContext : DbContext
{
    public static string JobbaSchema = "jobba";

    public JobbaDbContext()
    {
    }

    public JobbaDbContext(DbContextOptions<JobbaDbContext> options) : base(options)
    {
    }

    public DbSet<JobEntity> Jobs { get; set; }
    public DbSet<JobRegistration> JobRegistrations { get; set; }
    public DbSet<JobProgressEntity> JobProgress { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureJobRegistrations(modelBuilder);
        ConfigureJobs(modelBuilder);
        ConfigureJobProgress(modelBuilder);
    }

    private void ConfigureJobRegistrations(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<JobRegistration>();
        builder.ToTable("JobRegistrations", JobbaSchema);

        builder.HasIndex(x => x.JobName).IsUnique();

        builder.Property(x => x.JobType).HasConversion<EfTypeConverter>();
        builder.Property(x => x.JobParamsType).HasConversion<EfTypeConverter>();
        builder.Property(x => x.JobStateType).HasConversion<EfTypeConverter>();
        MapJson(builder, x => x.DefaultParams);
        MapJson(builder, x => x.DefaultState);

        builder.HasMany<JobEntity>()
            .WithOne()
            .HasForeignKey(x => x.JobRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<JobProgressEntity>()
            .WithOne()
            .HasForeignKey(x => x.JobRegistrationId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private void ConfigureJobs(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<JobEntity>();
        builder.ToTable("Jobs", JobbaSchema);

        builder.HasIndex(x => x.Status);

        builder.HasMany<JobProgressEntity>()
            .WithOne()
            .HasForeignKey(x => x.JobId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        MapJson(builder, x => x.JobParameters);
        MapJson(builder, x => x.JobState);
        builder.ComplexProperty(x => x.SystemInfo, x => x.IsRequired());

        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.LastProgressPercentage).HasPrecision(5, 2);
    }

    private void ConfigureJobProgress(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<JobProgressEntity>();
        builder.ToTable("JobProgress", JobbaSchema);

        builder.ToTable("JobProgress", JobbaSchema);
        MapJson(builder, x => x.JobState);
        builder.Property(x => x.Progress).HasPrecision(5, 2);
    }


    private static void MapJson<T, TProperty>(EntityTypeBuilder<T> builder,
        Expression<Func<T, TProperty>> func) where T : class
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All
        };

        builder.Property(func)
            .HasConversion(new EfJsonConverter<TProperty>(settings))
            .Metadata
            .SetValueComparer(new EfJsonComparer<TProperty>(settings));
    }

    public static async Task InitializeAsync(JobbaDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
    }
}
