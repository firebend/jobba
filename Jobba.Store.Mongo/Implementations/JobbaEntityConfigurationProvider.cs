using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaEntityConfigurationProvider<TEntity> : IJobbaEntityConfigurationProvider<TEntity>
    {
        private readonly JobbaEntityConfiguration _configuration;

        public JobbaEntityConfigurationProvider(JobbaEntityConfiguration configuration)
        {
            _configuration = configuration;
        }

        public JobbaEntityConfiguration GetConfiguration() => _configuration;
    }
}
