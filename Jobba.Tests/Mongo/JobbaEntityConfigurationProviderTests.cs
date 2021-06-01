using FluentAssertions;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Mongo
{
    [TestClass]
    public class JobbaEntityConfigurationProviderTests
    {
        [TestMethod]
        public void Jobba_Entity_Configuration_Provider_Should_Provide_Configuration()
        {
            //arrange
            var expected = new JobbaEntityConfiguration {Collection = "fake collection", Database = "fake db"};

            var provider = new JobbaEntityConfigurationProvider<object>(expected);

            //act
            var config = provider.GetConfiguration();

            //assert
            config.Should().BeEquivalentTo(expected);
        }
    }
}
