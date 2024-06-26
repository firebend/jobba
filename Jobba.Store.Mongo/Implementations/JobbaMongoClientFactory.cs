using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoClientFactory : IJobbaMongoClientFactory
{
    private readonly ILogger _logger;

    public JobbaMongoClientFactory(ILogger<JobbaMongoClientFactory> logger)
    {
        _logger = logger;
    }

    public IMongoClient CreateClient(string connectionString, bool enableLogging)
    {
        var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);

        if (enableLogging)
        {
            mongoClientSettings.ClusterConfigurator = Configurator;
        }

        return new MongoClient(mongoClientSettings);
    }

    private void Configurator(ClusterBuilder cb)
    {
        cb.Subscribe<CommandStartedEvent>(e =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("MONGO: {CommandName} - {Command}", e.CommandName, e.Command.ToJson());
            }
        });

        cb.Subscribe<CommandSucceededEvent>(e =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("SUCCESS: {CommandName}({Duration}) - {Reply}", e.CommandName, e.Duration, e.Reply.ToJson());
            }
        });

        cb.Subscribe<CommandFailedEvent>(e =>
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(e.Failure, "ERROR: {CommandName}({Duration})", e.CommandName, e.Duration);
            }
        });
    }
}
