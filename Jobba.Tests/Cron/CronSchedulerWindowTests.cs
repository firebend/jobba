using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Implementations;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobba.Tests.Cron;

[TestClass]
public class CronSchedulerWindowTests
{
    private const string EveryMinute = "* * * * *";
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private static readonly DateTimeOffset Noon = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    // The hosted service keeps whatever phase its first tick had, so a window can end a few
    // seconds past the minute. The occurrence inside it must still be enqueued.
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(14)]
    public async Task Should_Enqueue_When_The_Occurrence_Is_Inside_An_Off_Phase_Window(int secondsPastMinute)
    {
        var max = Noon.AddSeconds(secondsPastMinute);
        var sut = new Sut(previous: Noon.AddMinutes(-1));

        await sut.TickAsync(max);

        sut.EnqueuedCount.Should().Be(1);
        sut.SavedPrevious.Should().Be(Noon);
        sut.SavedNext.Should().Be(Noon.AddMinutes(1));
    }

    [TestMethod]
    [DataRow(16)]
    [DataRow(30)]
    [DataRow(59)]
    public async Task Should_Not_Enqueue_When_No_Occurrence_Is_Inside_The_Window(int secondsPastMinute)
    {
        var sut = new Sut(previous: Noon);

        await sut.TickAsync(Noon.AddSeconds(secondsPastMinute));

        sut.EnqueuedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Should_Enqueue_A_Job_That_Has_Never_Run_When_The_Occurrence_Is_Inside_The_Window()
    {
        var sut = new Sut(previous: null);

        await sut.TickAsync(Noon.AddSeconds(5));

        sut.EnqueuedCount.Should().Be(1);
    }

    // The window [12:00:00, 12:00:15] contains 12:00, but 12:00 already ran.
    [TestMethod]
    public async Task Should_Not_Enqueue_An_Occurrence_That_Already_Ran()
    {
        var sut = new Sut(previous: Noon);

        await sut.TickAsync(Noon.AddSeconds(15));

        sut.EnqueuedCount.Should().Be(0);
    }

    // Windows share their boundary: [11:59:45, 12:00:00] and [12:00:00, 12:00:15] both contain 12:00:00.
    [TestMethod]
    public async Task Should_Not_Enqueue_The_Same_Occurrence_Twice_On_A_Shared_Boundary()
    {
        var sut = new Sut(previous: Noon.AddMinutes(-1));

        await sut.TickAsync(Noon);
        sut.EnqueuedCount.Should().Be(1);

        sut.Registration.PreviousExecutionDate = sut.SavedPrevious;
        await sut.TickAsync(Noon.AddSeconds(15));

        sut.EnqueuedCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Should_Enqueue_Once_Per_Minute_Across_Consecutive_Off_Phase_Ticks()
    {
        var sut = new Sut(previous: Noon.AddMinutes(-1));

        for (var tick = Noon.AddSeconds(5); tick < Noon.AddMinutes(5); tick = tick.Add(Interval))
        {
            await sut.TickAsync(tick);
            sut.Registration.PreviousExecutionDate = sut.SavedPrevious ?? sut.Registration.PreviousExecutionDate;
        }

        // Ticks end at 12:00:05 .. 12:04:50; the windows contain 12:00 .. 12:04.
        sut.EnqueuedCount.Should().Be(5);
    }

    [TestMethod]
    [DataRow(null, true)]
    [DataRow(-60, true)]
    [DataRow(-5, true)]
    [DataRow(0, false)]
    [DataRow(60, false)]
    public void ShouldExecute_Should_Require_The_Occurrence_To_Be_After_The_Previous_Run(int? previousOffsetSeconds,
        bool expected)
    {
        DateTimeOffset? previous = previousOffsetSeconds is null ? null : Noon.AddSeconds(previousOffsetSeconds.Value);

        CronScheduler.ShouldExecute(previous, Noon, Noon.AddSeconds(-10), Noon.AddSeconds(5))
            .Should().Be(expected);
    }

    [TestMethod]
    [DataRow(-11)]
    [DataRow(6)]
    public void ShouldExecute_Should_Be_False_Outside_The_Window(int occurrenceOffsetSeconds)
    {
        CronScheduler.ShouldExecute(null, Noon.AddSeconds(occurrenceOffsetSeconds),
                Noon.AddSeconds(-10), Noon.AddSeconds(5))
            .Should().BeFalse();
    }

    [TestMethod]
    public async Task Should_Save_The_Run_Dates_Of_Queued_Jobs_When_Another_Job_Fails_To_Enqueue()
    {
        var sut = new Sut(previous: Noon.AddMinutes(-1));
        var failing = sut.AddRegistration(previous: Noon.AddMinutes(-1), failOnEnqueue: true);

        await sut.TickAsync(Noon.AddSeconds(5));

        sut.EnqueuedCount.Should().Be(1);
        sut.SavedPrevious.Should().Be(Noon);
        sut.SavedRegistrationIds.Should().NotContain(failing.Id);
    }

    private sealed class Sut
    {
        private readonly CronScheduler _scheduler;
        private readonly Mock<IJobScheduler> _jobScheduler = new();

        public JobRegistration Registration { get; }
        public int EnqueuedCount { get; private set; }
        public DateTimeOffset? SavedNext { get; private set; }
        public DateTimeOffset? SavedPrevious { get; private set; }
        public List<Guid> SavedRegistrationIds { get; } = [];
        private readonly List<JobRegistration> _registrations = [];

        public Sut(DateTimeOffset? previous)
        {
            Registration = AddRegistration(previous, failOnEnqueue: false);

            var store = new Mock<IJobRegistrationStore>();
            store
                .Setup(x => x.GetJobsWithCronExpressionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _registrations.ToList());
            store
                .Setup(x => x.UpdateNextAndPreviousInvocationDatesAsync(It.IsAny<Guid>(),
                    It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, DateTimeOffset?, DateTimeOffset?, CancellationToken>((id, next, prev, _) =>
                {
                    SavedRegistrationIds.Add(id);

                    if (id != Registration.Id)
                    {
                        return;
                    }

                    SavedNext = next;
                    SavedPrevious = prev;
                })
                .Returns(Task.CompletedTask);

            var lockService = new Mock<IJobLockService>();
            lockService
                .Setup(x => x.LockSystemAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new SystemLockResult(true, Mock.Of<IDisposable>()));

            var registry = new CronJobTypeRegistry();
            registry.RegisterJobType<CronSchedulerTests.CronSchedulerTestsJob, DefaultJobParams, DefaultJobState>();

            _scheduler = new CronScheduler(_jobScheduler.Object, NullLogger<CronScheduler>.Instance,
                new CronService(), store.Object, lockService.Object, registry);
        }

        public JobRegistration AddRegistration(DateTimeOffset? previous, bool failOnEnqueue)
        {
            var registration = new JobRegistration
            {
                Id = Guid.NewGuid(),
                JobName = "window-test",
                SystemMoniker = "test",
                JobType = typeof(CronSchedulerTests.CronSchedulerTestsJob),
                CronExpression = EveryMinute,
                PreviousExecutionDate = previous,
            };

            var setup = _jobScheduler.Setup(x => x.ScheduleJobAsync(registration.Id, It.IsAny<DefaultJobParams>(),
                It.IsAny<DefaultJobState>(), It.IsAny<CancellationToken>()));

            if (failOnEnqueue)
            {
                setup.ThrowsAsync(new InvalidOperationException("enqueue failed"));
            }
            else
            {
                setup.Callback(() => EnqueuedCount++)
                    .ReturnsAsync((JobInfo<DefaultJobParams, DefaultJobState>)null);
            }

            _registrations.Add(registration);
            return registration;
        }

        // Interval zero: HoldLockAsync has nothing left to wait for.
        public Task TickAsync(DateTimeOffset max)
            => _scheduler.EnqueueJobsAsync(
                new CronSchedulerContext(TimeSpan.Zero, max.Subtract(Interval), max, "test"),
                CancellationToken.None);
    }
}
