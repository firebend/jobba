using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Abstractions;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Core.Builders;

[TestClass]
public class JobbaBuilderTests
{
    [TestMethod]
    public void Jobba_Builder_Should_Build()
    {
        //arrange
        var serviceCollection = new ServiceCollection();
        var builder = new JobbaBuilder(serviceCollection, "fake");
        var mockProgressStore = new Mock<IJobProgressStore>();
        serviceCollection.AddSingleton(mockProgressStore.Object);

        //act
        builder.AddJob<FooJob, FooParams, FooState>("fake");
        var provider = serviceCollection.BuildServiceProvider();

        //assert
        var registrations = provider.GetServices<JobRegistration>().ToArray();
        registrations.Length.Should().Be(1);
        registrations.First().JobType.Should().Be<FooJob>();
        registrations.First().JobStateType.Should().Be<FooState>();
        registrations.First().JobParamsType.Should().Be<FooParams>();
    }

    [TestMethod]
    public void Jobba_Builder_Should_Throw_For_Duplicate_Job_Name()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        var builder = new JobbaBuilder(serviceCollection, "fake");
        var mockProgressStore = new Mock<IJobProgressStore>();
        serviceCollection.AddSingleton(mockProgressStore.Object);
        builder.AddJob<FooJob, FooParams, FooState>("dup-name");

        // act
        var action = () => builder.AddJob<BarJob, BarParams, BarState>("dup-name");

        // assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [TestMethod]
    public void Jobba_Builder_Should_Throw_For_Duplicate_Job_Type()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        var builder = new JobbaBuilder(serviceCollection, "fake");
        var mockProgressStore = new Mock<IJobProgressStore>();
        serviceCollection.AddSingleton(mockProgressStore.Object);
        builder.AddJob<FooJob, FooParams, FooState>("job-1");

        // act
        var action = () => builder.AddJob<FooJob, FooParams, FooState>("job-2");

        // assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    private class FooState : IJobState;

    private class FooParams : IJobParams;

    private class FooJob : AbstractJobBaseClass<FooParams, FooState>
    {
        public FooJob(IJobProgressStore progressStore) : base(progressStore)
        {
        }

        public override string JobName => "Jerb";

        protected override Task OnStartAsync(JobStartContext<FooParams, FooState> jobStartContext, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private class BarState : IJobState;

    private class BarParams : IJobParams;

    private class BarJob : AbstractJobBaseClass<BarParams, BarState>
    {
        public BarJob(IJobProgressStore progressStore) : base(progressStore)
        {
        }

        public override string JobName => "BarJerb";

        protected override Task OnStartAsync(JobStartContext<BarParams, BarState> jobStartContext, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
