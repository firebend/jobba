using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Abstractions
{
    public abstract class JobbaMongoClient
    {
        protected JobbaMongoClient(IMongoClient client,
            ILogger logger)
        {
            Client = client;
            Logger = logger;
        }

        protected IMongoClient Client { get; }

        protected ILogger Logger { get; }

        protected Task RetryErrorAsync(Func<Task> method) => RetryErrorAsync(async () =>
        {
            await method();
            return true;
        });

        protected async Task<TReturn> RetryErrorAsync<TReturn>(Func<Task<TReturn>> method, bool retry = true)
        {
            try
            {
                return await method();
            }
            catch (Exception ex)
            {
                if (retry)
                {
                    await Task.Delay(100);
                    return await RetryErrorAsync(method, false);
                }

                Logger?.LogError(ex, "Error querying Document Store: \"{Message}\"", ex.Message);

                throw;
            }
        }
    }
}
