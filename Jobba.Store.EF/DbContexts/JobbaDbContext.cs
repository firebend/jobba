using System;
using System.Linq.Expressions;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Jobba.Store.EF.DbContexts;

public class JobbaDbContext(DbContextOptions<JobbaDbContext> options) : DbContext(options)
{
    public static string JobbaSchema = "jobba";
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

        builder.Property(x => x.JobType).HasConversion<EfTypeConverter>();
        builder.Property(x => x.JobParamsType).HasConversion<EfTypeConverter>();
        builder.Property(x => x.JobStateType).HasConversion<EfTypeConverter>();
        MapJson(builder, x => x.DefaultParams);
        MapJson(builder, x => x.DefaultState);
    }

    private void ConfigureJobs(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<JobEntity>();
        builder.ToTable("Jobs", JobbaSchema);

        builder.HasOne<JobRegistration>()
            .WithMany()
            .HasForeignKey(x => x.JobRegistrationId)
            .IsRequired();

        MapJson(builder, x => x.JobParameters);
        MapJson(builder, x => x.JobState);
        builder.OwnsOne(x => x.SystemInfo);

    }

    private void ConfigureJobProgress(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<JobProgressEntity>();
        builder.ToTable("JobProgress", JobbaSchema);

        builder.HasOne<JobEntity>()
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .IsRequired();

        builder.HasOne<JobRegistration>()
            .WithMany()
            .HasForeignKey(x => x.JobRegistrationId)
            .IsRequired();

        builder.ToTable("JobProgress", JobbaSchema);
        MapJson(builder, x => x.JobState);
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
}
