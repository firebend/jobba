using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Abstractions;

public class JobbaMongoEntityClient<TEntity> : JobbaMongoClient
{
    public JobbaMongoEntityClient(IMongoClient client,
        ILogger logger,
        IJobbaEntityConfigurationProvider<TEntity> entityConfigurationProvider,
        IJobbaMongoRetryService retryService) : base(client, logger, retryService)
    {
        EntityConfiguration = entityConfigurationProvider.GetConfiguration();
    }

    protected JobbaEntityConfiguration EntityConfiguration { get; }

    protected IMongoCollection<TEntity> GetCollection()
    {
        var database = Client.GetDatabase(EntityConfiguration.Database);

        return database.GetCollection<TEntity>(EntityConfiguration.Collection);
    }
}
