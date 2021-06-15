using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Abstractions;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Builders
{
    [TestClass]
    public class JobbaBuilderTests
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

            public override string JobName => "Jerb";
        }

        [TestMethod]
        public void Jobba_Builder_Should_Build()
        {
            //arrange
            var serviceCollection = new ServiceCollection();
            var builder = new JobbaBuilder(serviceCollection);
            var mockProgressStore = new Mock<IJobProgressStore>();
            serviceCollection.AddSingleton(mockProgressStore.Object);

            //act
            builder.AddJob<FooJob, FooParams, FooState>();
            var provider = serviceCollection.BuildServiceProvider();

            //assert
            serviceCollection.Count.Should().BeGreaterThan(13);
            provider.GetService<FooJob>().Should().NotBeNull();
        }
    }
}
