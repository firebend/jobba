using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Cron.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Cron;

[TestClass]
public class CronJobTypeRegistryTests
{
    [TestMethod]
    public void RegisterJobType_Should_Throw_When_Same_JobType_Registers_Twice_With_Same_Params_And_State()
    {
        // arrange
        var registry = new CronJobTypeRegistry();
        registry.RegisterJobType<MultiInterfaceJob, FooParams, FooState>();

        // act
        var action = () => registry.RegisterJobType<MultiInterfaceJob, FooParams, FooState>();

        // assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be registered twice*");
    }

    [TestMethod]
    public void RegisterJobType_Should_Throw_When_Same_JobType_Uses_Different_Params_And_State()
    {
        // arrange
        var registry = new CronJobTypeRegistry();
        registry.RegisterJobType<MultiInterfaceJob, FooParams, FooState>();

        // act
        var action = () => registry.RegisterJobType<MultiInterfaceJob, BarParams, BarState>();

        // assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be registered twice*");
    }

    private sealed class FooParams : IJobParams;

    private sealed class FooState : IJobState;

    private sealed class BarParams : IJobParams;

    private sealed class BarState : IJobState;

    private sealed class MultiInterfaceJob : IJob<FooParams, FooState>, IJob<BarParams, BarState>
    {
        public string JobName => nameof(MultiInterfaceJob);

        Task IJob<FooParams, FooState>.StartAsync(JobStartContext<FooParams, FooState> jobStartContext, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task IJob<BarParams, BarState>.StartAsync(JobStartContext<BarParams, BarState> jobStartContext, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
