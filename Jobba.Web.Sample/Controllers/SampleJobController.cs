using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jobba.Web.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleJobController : ControllerBase
    {
        private readonly IJobScheduler _jobScheduler;
        private readonly IJobStore _jobStore;

        public SampleJobController(IJobScheduler jobScheduler, IJobStore jobStore)
        {
            _jobScheduler = jobScheduler;
            _jobStore = jobStore;
        }

        [HttpPost]
        public async Task<IActionResult> CreateJobAsync(CancellationToken cancellationToken)
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

            var job = await _jobScheduler.ScheduleJobAsync(request, cancellationToken);

            return Ok(job);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelJobAsync([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await _jobScheduler.CancelJobAsync(id, cancellationToken);
            return Ok();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetJobByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var job = await _jobStore.GetJobByIdAsync<SampleWebJobParameters, SampleWebJobState>(id, cancellationToken);
            return Ok(job);
        }
    }
}
