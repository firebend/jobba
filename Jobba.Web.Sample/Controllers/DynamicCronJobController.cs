using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jobba.Web.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class DynamicCronJobController : ControllerBase
{
    private readonly IJobOrchestrationService _jobOrchestrationService;

    public DynamicCronJobController(IJobOrchestrationService jobOrchestrationService)
    {
        _jobOrchestrationService = jobOrchestrationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CancellationToken cancellationToken)
    {
        var request = new JobOrchestrationRequest<DynamicCronJob, CronParameters, CronState>(
            $"{DynamicCronJob.Name}-{Guid.NewGuid()}",
            "A dynamic cron job",
            "* * * * *",
            new() { StartDate = DateTimeOffset.UtcNow },
            new() { Phrase = $"I Like Turtles {Guid.NewGuid()}" },
            TimeZoneInfo.Local.Id);

        var result = await _jobOrchestrationService.OrchestrateJobAsync(request, cancellationToken);

        while (DynamicCronJobStatics.Runs.ContainsKey(result.Registration.Id) is false)
        {
            await Task.Delay(100, cancellationToken);
        }

        return Ok();
    }
}
