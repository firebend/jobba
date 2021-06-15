using FluentAssertions;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Redis;
using Jobba.Redis.Implementations;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Redis
{
    [TestClass]
    public class LitRedisJobbaExtensionsTests
    {
        [TestMethod]
        public void Lit_Redis_Jobba_Extension_Should_Register_Services()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddJobba(jobba => jobba.UsingLitRedis("connString"));

            using var provider = serviceCollection.BuildServiceProvider();

            provider.GetService<IJobLockService>().Should().NotBeNull().And.BeOfType<LitRedisJobLockService>();
            provider.GetService<ILitRedisDistributedLock>().Should().NotBeNull();
            provider.GetService<ILitRedisDistributedLockService>().Should().NotBeNull();
        }
    }
}
