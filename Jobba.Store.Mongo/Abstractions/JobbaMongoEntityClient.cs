using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Abstractions
{
    public class JobbaMongoEntityClient<TEntity> : JobbaMongoClient
    {
        protected JobbaEntityConfiguration EntityConfiguration { get; }

        public JobbaMongoEntityClient(IMongoClient client,
            ILogger logger,
            IJobbaEntityConfigurationProvider<TEntity> entityConfigurationProvider) : base(client, logger)
        {
            EntityConfiguration = entityConfigurationProvider.GetConfiguration();
        }

        protected IMongoCollection<TEntity> GetCollection()
        {
            var database = Client.GetDatabase(EntityConfiguration.Database);

            return database.GetCollection<TEntity>(EntityConfiguration.Collection);
        }
    }
}
