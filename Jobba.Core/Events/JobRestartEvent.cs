using System;

namespace Jobba.Core.Events;

public class JobRestartEvent
{
    /// <summary>
    /// The job id.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's parameters type name.
    /// </summary>
    public string JobParamsTypeName { get; set; }

    /// <summary>
    /// The job's state type name.
    /// </summary>
    public string JobStateTypeName { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }

    public JobRestartEvent()
    {

    }

    public JobRestartEvent(Guid jobId, string jobParamsTypeName, string jobStateTypeName, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobParamsTypeName = jobParamsTypeName;
        JobStateTypeName = jobStateTypeName;
        JobRegistrationId = jobRegistrationId;
    }
}
