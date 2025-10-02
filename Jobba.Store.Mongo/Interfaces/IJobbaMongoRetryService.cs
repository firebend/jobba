using System;
using System.Threading.Tasks;

namespace Jobba.Store.Mongo.Interfaces;

public interface IJobbaMongoRetryService
{
    public Task<TReturn> RetryErrorAsync<TReturn>(Func<Task<TReturn>> method, int maxTries);
}
