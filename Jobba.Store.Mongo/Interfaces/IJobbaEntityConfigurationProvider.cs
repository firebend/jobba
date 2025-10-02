using Jobba.Store.Mongo.Models;

namespace Jobba.Store.Mongo.Interfaces;

public interface IJobbaEntityConfigurationProvider<TEntity>
{
    public JobbaEntityConfiguration GetConfiguration();
}
