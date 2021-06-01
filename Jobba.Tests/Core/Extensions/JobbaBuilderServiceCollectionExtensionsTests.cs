using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Abstractions;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Extensions
{
    [TestClass]
    public class JobbaBuilderServiceCollectionExtensionsTests
    {
        private class FooState
        {

        }

        private class FooParams
        {

        }

        private class FooJob : AbstractJobBaseClass<FooParams, FooState>
        {
            public FooJob(IJobProgressStore progressStore) : base(progressStore)
            {
            }

            protected override Task OnStartAsync(JobStartContext<FooParams, FooState> jobStartContext, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        [TestMethod]
        public void Jobba_Builder_Service_Collection_Extensions_Should_Add_Jobba()
        {
            //arrange
            var serviceCollection = new ServiceCollection();
            var mockProgressStore = new Mock<IJobProgressStore>();
            serviceCollection.AddSingleton(mockProgressStore.Object);

            //act
            serviceCollection.AddJobba(builder =>
            {
                builder.AddJob<FooJob, FooParams, FooState>();
            });

            var provider = serviceCollection.BuildServiceProvider();

            //assert
            serviceCollection.Count.Should().BeGreaterThan(13);
            provider.GetService<FooJob>().Should().NotBeNull();

        }
    }
}
