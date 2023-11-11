using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jobba.Web.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class DynamicJobController : ControllerBase
{
    private readonly IJobOrchestrationService _jobOrchestrationService;

    public DynamicJobController(IJobOrchestrationService jobOrchestrationService)
    {
        _jobOrchestrationService = jobOrchestrationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CancellationToken cancellationToken)
    {
        var request = new JobOrchestrationRequest<DynamicJob, DefaultJobParams, DefaultJobState>(
            $"{DynamicJob.Name}-{Guid.NewGuid()}",
            "A dynamic job");

        var result = await _jobOrchestrationService.OrchestrateJobAsync(request, cancellationToken);

        while (DynamicJobStatics.Runs.ContainsKey(result.JobInfo.Id) is false)
        {
            await Task.Delay(100, cancellationToken);
        }

        return Ok();
    }
}
