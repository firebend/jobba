using System;
using System.Threading.Tasks;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations
{
    //todo: write tests
    public class JobbaMongoRetryService : IJobbaMongoRetryService
    {
        public async Task<TReturn> RetryErrorAsync<TReturn>(Func<Task<TReturn>> method, int maxTries)
        {
            var tries = 0;
            double delay = 100;
            TimeSpan delayTimeSpan;

            while (true)
            {
                try
                {
                    return await method();
                }
                catch
                {
                    tries++;

                    if (tries >= maxTries)
                    {
                        throw;
                    }

                    if (tries != 1)
                    {
                        delay = Math.Ceiling(Math.Pow(delay, 1.1));
                    }

                    delayTimeSpan = TimeSpan.FromMilliseconds(delay);
                    await Task.Delay(delayTimeSpan);
                }
            }
        }
    }
}
