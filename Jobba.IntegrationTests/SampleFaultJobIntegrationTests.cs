using System;
using System.Threading.Tasks;
using FluentAssertions;
using Flurl.Http;
using Jobba.Core.Models;
using Jobba.IntegrationTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.IntegrationTests;

[TestClass]
public class SampleFaultJobIntegrationTests
{
    private const string Url = "http://localhost:5000/samplefaultjob";

    private static async Task<T> WaitUntil<T>(Func<Task<T>> func,
        Func<T, bool> condition,
        int maxTimes = 20,
        TimeSpan? delay = null,
        string because = null)
    {
        var tries = 0;
        delay ??= TimeSpan.FromSeconds(1);

        while (tries < maxTimes)
        {
            try
            {
                var result = await func();

                if (condition(result))
                {
                    return result;
                }
            }
            finally
            {
                tries++;

                if (tries < maxTimes)
                {
                    await Task.Delay(delay.Value);
                }
            }
        }

        throw new Exception($"Couldn't get it. {because}");
    }

    [TestMethod]
    public async Task Should_Schedule_Sample_Fault_Job()
    {
        var job = await Url.PostAsync().ReceiveJson<JobInfo<SampleWebJobParameters, SampleWebJobState>>();
        job.Should().NotBeNull();
        job.FaultedReason.Should().BeNullOrWhiteSpace();
        job.CurrentNumberOfTries.Should().Be(1);
        job.IsOutOfRetry.Should().BeFalse();

        var inProgressJobInfo = await WaitUntil(
            () => $"{Url}/{job.Id}".GetJsonAsync<JobInfo<SampleWebJobParameters, SampleWebJobState>>(),
            x => x.Status == JobStatus.InProgress,
            because: "Job is not in progress"
        );
        inProgressJobInfo.Should().NotBeNull();
        inProgressJobInfo.Status.Should().Be(JobStatus.InProgress);

        var faultJobResponse = await $"{Url}/{job.Id}/fault".PostAsync();
        faultJobResponse.StatusCode.Should().Be(200);

        var faultJobInfo = await WaitUntil(
            () => $"{Url}/{job.Id}".GetJsonAsync<JobInfo<SampleWebJobParameters, SampleWebJobState>>(),
            x => x.Status == JobStatus.Faulted,
            because: "Job is not faulted"
        );

        faultJobInfo.Should().NotBeNull();
        faultJobInfo.Status.Should().Be(JobStatus.Faulted);

        var runResponse = await $"{Url}/{job.Id}/run".PostAsync();
        runResponse.StatusCode.Should().Be(200);

        var runJobInfo = await WaitUntil(
            () => $"{Url}/{job.Id}".GetJsonAsync<JobInfo<SampleWebJobParameters, SampleWebJobState>>(),
            x => x.Status == JobStatus.Completed,
            because: "Job is not faulted"
        );

        runJobInfo.Should().NotBeNull();
        runJobInfo.Status.Should().Be(JobStatus.Completed);
        runJobInfo.CurrentNumberOfTries.Should().BeGreaterThan(1);
        runJobInfo.CurrentState.Tries.Should().BeGreaterThan(1);
    }
}
