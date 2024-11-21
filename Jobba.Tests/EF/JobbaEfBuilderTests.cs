using System.Linq;
using FluentAssertions;
using Jobba.Core.Extensions;
using Jobba.Core.Models;
using Jobba.Store.EF.DbContexts;
using Jobba.Store.EF.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.EF;

[TestClass]
public class JobbaEfBuilderTests
{
    [TestMethod]
    public void Jobba_Ef_Builder_Should_Build()
    {
        //arrange
        var testContext = new EfTestContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        //act
        serviceCollection.AddJobba("fake", builder =>
        {
            builder.AddJob<TestModels.FooJob, TestModels.FooParams, TestModels.FooState>("fake");
            builder.UsingSqlite("DataSource=:memory:");
        });

        var provider = serviceCollection.BuildServiceProvider();

        //assert
        var registrations = provider.GetServices<JobRegistration>().ToArray();
        registrations.Length.Should().Be(1);
        registrations.First().JobType.Should().Be<TestModels.FooJob>();
        registrations.First().JobStateType.Should().Be<TestModels.FooState>();
        registrations.First().JobParamsType.Should().Be<TestModels.FooParams>();

        var dbContext = provider.GetRequiredService<JobbaDbContext>();
        dbContext.Database.IsSqlite().Should().BeTrue();
        testContext.Dispose();
    }
}
