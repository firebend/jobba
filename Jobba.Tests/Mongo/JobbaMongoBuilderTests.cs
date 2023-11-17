using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Abstractions;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.Mongo.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoBuilderTests
{
    [TestMethod]
    public void Jobba_Mongo_Builder_Should_Build()
    {
        //arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        //act
        serviceCollection.AddJobba("fake", builder =>
        {
            builder.AddJob<FooJob, FooParams, FooState>("fake");
            builder.UsingMongo("mongodb://localhost:27017/jobba", true);
        });

        var provider = serviceCollection.BuildServiceProvider();

        //assert
        serviceCollection.Count.Should().Be(37);
        var registrations = provider.GetServices<JobRegistration>().ToArray();
        registrations.Length.Should().Be(1);
        registrations.First().JobType.Should().Be<FooJob>();
        registrations.First().JobStateType.Should().Be<FooState>();
        registrations.First().JobParamsType.Should().Be<FooParams>();
    }

    private class FooState : IJobState
    {
    }

    private class FooParams : IJobParams
    {
    }

    private class FooJob : AbstractJobBaseClass<FooParams, FooState>
    {
        public FooJob(IJobProgressStore progressStore) : base(progressStore)
        {
        }

        public override string JobName => "Jerb";

        protected override Task OnStartAsync(JobStartContext<FooParams, FooState> jobStartContext, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
