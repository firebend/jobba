using MongoDB.Driver;

namespace Jobba.Store.Mongo.Interfaces
{
    public interface IJobbaMongoClientFactory
    {
        IMongoClient CreateClient(string connectionString, bool enableLogging);
    }
}
