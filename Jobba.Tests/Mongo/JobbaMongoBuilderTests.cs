using System.Linq;
using FluentAssertions;
using Jobba.Core.Extensions;
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
            builder.AddJob<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>("fake");
            builder.UsingMongo("mongodb://localhost:27017/jobba/?directConnection=true&appName=jobba-sample", true);
        });

        var provider = serviceCollection.BuildServiceProvider();

        //assert
        serviceCollection.Count.Should().Be(38);
        var registrations = provider.GetServices<JobRegistration>().ToArray();
        registrations.Length.Should().Be(1);
        registrations.First().JobType.Should().Be<TestModels.FooJob>();
        registrations.First().JobStateType.Should().Be<TestModels.FooState>();
        registrations.First().JobParamsType.Should().Be<TestModels.FooParams>();
    }
}
