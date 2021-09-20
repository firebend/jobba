using System;
using System.Threading.Tasks;
using FluentAssertions;
using Flurl.Http;
using Jobba.Core.Models;
using Jobba.IntegrationTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.IntegrationTests
{
    [TestClass]
    public class SampleWebJobIntegrationTests
    {
        private const string Url = "http://localhost:5000/samplejob";

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
        public async Task Should_Schedule_Sample_Job()
        {
            var job = await Url.PostAsync().ReceiveJson<JobInfo<SampleWebJobParameters, SampleWebJobState>>();
            job.Should().NotBeNull();
            job.FaultedReason.Should().BeNullOrWhiteSpace();
            job.CurrentNumberOfTries.Should().Be(1);
            job.IsOutOfRetry.Should().BeFalse();

            var inProgressJobInfo = await WaitUntil(
                () => $"{Url}/{job.Id}".GetJsonAsync<JobInfo<SampleWebJobParameters, SampleWebJobState>>(),
                x => x.Status == JobStatus.InProgress,
                because:"Job is not in progress"
            );
            inProgressJobInfo.Should().NotBeNull();
            inProgressJobInfo.Status.Should().Be(JobStatus.InProgress);

            var cancelJobResponse = await $"{Url}/{job.Id}/cancel".PostAsync();
            cancelJobResponse.StatusCode.Should().Be(200);

            var cancelledJobInfo = await WaitUntil(
                () => $"{Url}/{job.Id}".GetJsonAsync<JobInfo<SampleWebJobParameters, SampleWebJobState>>(),
                x => x.Status == JobStatus.Cancelled,
                because:"Job is not cancelled"
            );

            cancelledJobInfo.Should().NotBeNull();
            cancelledJobInfo.Status.Should().Be(JobStatus.Cancelled);
        }
    }
}
