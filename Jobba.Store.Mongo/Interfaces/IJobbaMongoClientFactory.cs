using MongoDB.Driver;

namespace Jobba.Store.Mongo.Interfaces;

public interface IJobbaMongoClientFactory
{
    public IMongoClient CreateClient(string connectionString, bool enableLogging);
}
