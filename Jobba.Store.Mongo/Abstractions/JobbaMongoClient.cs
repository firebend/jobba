using System;
using System.Threading.Tasks;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Abstractions
{
    public abstract class JobbaMongoClient
    {
        protected JobbaMongoClient(IMongoClient client,
            ILogger logger,
            IJobbaMongoRetryService retryService)
        {
            Client = client;
            Logger = logger;
            RetryService = retryService;
        }

        protected IJobbaMongoRetryService RetryService { get;}

        protected IMongoClient Client { get; }

        protected ILogger Logger { get; }

        protected Task RetryErrorAsync(Func<Task> method) => RetryErrorAsync(async () =>
        {
            await method();
            return true;
        });

        protected async Task<TReturn> RetryErrorAsync<TReturn>(Func<Task<TReturn>> method, int maxTries = 7)
        {
            try
            {
                return await RetryService.RetryErrorAsync(method, maxTries);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error querying Document Store: \"{Message}\"", ex.Message);
                throw;
            }
        }
    }
}
