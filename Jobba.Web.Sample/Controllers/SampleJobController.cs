using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jobba.Web.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleJobController : ControllerBase
    {

        private readonly IJobScheduler _jobScheduler;

        public SampleJobController(IJobScheduler jobScheduler)
        {
            _jobScheduler = jobScheduler;
        }

        [HttpPost]
        public async Task<IActionResult> Get()
        {
            var request = new JobRequest<SampleWebJobParameters, SampleWebJobState>
            {
                Description = "A Sample Job",
                JobParameters = new SampleWebJobParameters { Greeting = "Hello" },
                JobType = typeof(SampleWebJob),
                InitialJobState = new SampleWebJobState { Tries = 0 },
                JobWatchInterval = TimeSpan.FromSeconds(10),
                MaxNumberOfTries = 100
            };

            await _jobScheduler.ScheduleJobAsync(request, default);

            // var cancelJobRequest = new JobRequest<object, object>
            // {
            //     Description = "A Sample Job that should get cancelled",
            //     JobParameters = new object(),
            //     JobType = typeof(SampleWebJobCancel),
            //     InitialJobState = new object(),
            //     JobWatchInterval = TimeSpan.FromSeconds(2),
            //     MaxNumberOfTries = 100
            // };

            // var jobToCancel = await _jobScheduler.ScheduleJobAsync(cancelJobRequest, default);
            // // ReSharper disable once UnusedVariable
            // var jobToRunUntilClose = await _jobScheduler.ScheduleJobAsync(cancelJobRequest, default);
            // await Task.Delay(TimeSpan.FromSeconds(5), default);
            // await _jobScheduler.CancelJobAsync(jobToCancel.Id, default);
            return Ok("Job Started");
        }
    }
}
