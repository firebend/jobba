using Jobba.Core.Builders;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Builders;

public class JobbaMongoBuilder
{
    public JobbaMongoBuilder(JobbaBuilder jobbaBuilder,
        string connectionString,
        bool enableCommandLogging)
    {
        Builder = jobbaBuilder;
        MongoUrl = new MongoUrl(connectionString);

        RegisterJobbaRequiredStores(jobbaBuilder);
        RegisterMongoRequiredServices(jobbaBuilder, connectionString, enableCommandLogging);

        WithJobCollection("Jobs");
        WithJobProgressCollection("JobProgress");
        WithJobRegistrationCollection("JobRegistrations");
    }

    private static void RegisterMongoRequiredServices(JobbaBuilder jobbaBuilder,
            string connectionString,
            bool enableCommandLogging)
    {
        jobbaBuilder.Services.TryAddScoped<IJobbaMongoClientFactory, JobbaMongoClientFactory>();

        jobbaBuilder.Services.AddSingleton(provider =>
        {
            using var scope = provider.CreateScope();
            var factory = scope.ServiceProvider.GetService<IJobbaMongoClientFactory>();
            var client = factory?.CreateClient(connectionString, enableCommandLogging);
            return client;
        });

        jobbaBuilder.Services.TryAddScoped<IJobbaMongoRepository<JobEntity>, JobbaMongoRepository<JobEntity>>();
        jobbaBuilder.Services.TryAddScoped<IJobbaMongoRepository<JobProgressEntity>, JobbaMongoRepository<JobProgressEntity>>();
        jobbaBuilder.Services.TryAddScoped<IJobbaMongoRepository<JobRegistration>, JobbaMongoRepository<JobRegistration>>();

        jobbaBuilder.Services.TryAddScoped<IJobbaMongoRetryService, JobbaMongoRetryService>();
    }

    private static void RegisterJobbaRequiredStores(JobbaBuilder jobbaBuilder)
    {
        jobbaBuilder.Services.TryAddScoped<IJobListStore, JobbaMongoJobListStore>();
        jobbaBuilder.Services.TryAddScoped<IJobProgressStore, JobbaMongoJobProgressStore>();
        jobbaBuilder.Services.TryAddScoped<IJobStore, JobbaMongoJobStore>();
        jobbaBuilder.Services.TryAddScoped<IJobCleanUpStore, JobbaMongoCleanUpStore>();
        jobbaBuilder.Services.TryAddScoped<IJobRegistrationStore, JobbaMongoJobRegistrationStore>();
    }

    public JobbaBuilder Builder { get; }

    public MongoUrl MongoUrl { get; }

    private string GetDatabaseName(string database) => database ?? MongoUrl.DatabaseName ?? "Jobba";

    private void RegisterEntityConfiguration<TEntity>(JobbaEntityConfiguration configuration) =>
        Builder.Services.RegisterReplace<IJobbaEntityConfigurationProvider<TEntity>>(new JobbaEntityConfigurationProvider<TEntity>(configuration));

    private void RegisterEntityConfiguration<TEntity, TProvider>()
        where TProvider : IJobbaEntityConfigurationProvider<TEntity> =>
        Builder.Services.RegisterReplace<IJobbaEntityConfigurationProvider<TEntity>, TProvider>();

    public JobbaMongoBuilder WithJobCollection(string name, string database = null)
    {
        var configuration = new JobbaEntityConfiguration
        {
            Collection = name,
            Database = GetDatabaseName(database)
        };
        RegisterEntityConfiguration<JobEntity>(configuration);
        return this;
    }

    public JobbaMongoBuilder WithJobCollection<TProvider>()
        where TProvider : IJobbaEntityConfigurationProvider<JobEntity>
    {
        RegisterEntityConfiguration<JobEntity, TProvider>();
        return this;
    }

    public JobbaMongoBuilder WithJobProgressCollection(string name, string database = null)
    {
        var configuration = new JobbaEntityConfiguration
        {
            Collection = name,
            Database = GetDatabaseName(database)
        };
        RegisterEntityConfiguration<JobProgressEntity>(configuration);
        return this;
    }

    public JobbaMongoBuilder WithJobProgressCollection<TProvider>()
        where TProvider : IJobbaEntityConfigurationProvider<JobProgressEntity>
    {
        RegisterEntityConfiguration<JobProgressEntity, TProvider>();
        return this;
    }

    public JobbaMongoBuilder WithJobRegistrationCollection(string name, string database = null)
    {
        var configuration = new JobbaEntityConfiguration
        {
            Collection = name,
            Database = GetDatabaseName(database)
        };
        RegisterEntityConfiguration<JobRegistration>(configuration);
        return this;
    }

    public JobbaMongoBuilder WithJobRegistration<TProvider>()
        where TProvider : IJobbaEntityConfigurationProvider<JobRegistration>
    {
        RegisterEntityConfiguration<JobRegistration, TProvider>();
        return this;
    }
}
