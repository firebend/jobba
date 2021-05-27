using Jobba.Core.Builders;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Builders
{
    //todo:test
    public class JobbaMongoBuilder
    {
        public JobbaBuilder Builder { get; }

        public MongoUrl MongoUrl { get; }

        public JobbaMongoBuilder(JobbaBuilder jobbaBuilder,
            string connectionString,
            bool enableCommandLogging)
        {
            Builder = jobbaBuilder;

            jobbaBuilder.Services.TryAddScoped<IJobbaMongoClientFactory, JobbaMongoClientFactory>();

            jobbaBuilder.Services.AddSingleton(provider =>
            {
                var factory = provider.GetService<IJobbaMongoClientFactory>();
                var client = factory?.CreateClient(connectionString, enableCommandLogging);
                return client;
            });

            MongoUrl = new MongoUrl(connectionString);

            jobbaBuilder.Services.TryAddScoped<IJobListStore, JobbaMongoJobListStore>();
            jobbaBuilder.Services.TryAddScoped<IJobProgressStore, JobbaMongoJobProgressStore>();
            jobbaBuilder.Services.TryAddScoped<IJobStore, JobbaMongoJobStore>();
            jobbaBuilder.Services.TryAddScoped<IJobbaMongoRepository<JobEntity>, JobbaMongoRepository<JobEntity>>();
            jobbaBuilder.Services.TryAddScoped<IJobbaMongoRepository<JobProgressEntity>, JobbaMongoRepository<JobProgressEntity>>();
            jobbaBuilder.Services.TryAddScoped<IJobbaMongoRetryService, JobbaMongoRetryService>();

            WithJobCollection("Jobs");
            WithJobProgressCollection("JobProgress");
        }

        private string GetDatabaseName(string database) => database ?? MongoUrl.DatabaseName ?? "Jobba";

        private void RegisterEntityConfiguration<TEntity>(JobbaEntityConfiguration configuration)
        {
            var serviceDescriptor = new ServiceDescriptor(
                typeof(IJobbaEntityConfigurationProvider<TEntity>),
                new JobbaEntityConfigurationProvider<TEntity>(configuration));

            Builder.Services.Replace(serviceDescriptor);
        }

        private void RegisterEntityConfiguration<TEntity, TProvider>()
            where TProvider : IJobbaEntityConfigurationProvider<TEntity>
        {
            var serviceDescriptor = new ServiceDescriptor(
                typeof(IJobbaEntityConfigurationProvider<TEntity>),
                typeof(TProvider));

            Builder.Services.Replace(serviceDescriptor);
        }

        public JobbaMongoBuilder WithJobCollection(string name, string database = null)
        {
            var configuration = new JobbaEntityConfiguration {Collection = name, Database = GetDatabaseName(database)};
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
            var configuration = new JobbaEntityConfiguration {Collection = name, Database = GetDatabaseName(database)};
            RegisterEntityConfiguration<JobProgressEntity>(configuration);
            return this;
        }

        public JobbaMongoBuilder WithJobProgressCollection<TProvider>()
            where TProvider : IJobbaEntityConfigurationProvider<JobProgressEntity>
        {
            RegisterEntityConfiguration<JobProgressEntity, TProvider>();
            return this;
        }
    }
}
