using System;

namespace Jobba.Core.Events;

public class JobWatchEvent
{
    public JobWatchEvent()
    {
    }

    public JobWatchEvent(Guid jobId, string paramsTypeName, string stateTypeName, Guid jobRegistrationId)
    {
        JobId = jobId;
        ParamsTypeName = paramsTypeName;
        StateTypeName = stateTypeName;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     The id of the job that needs to be watched.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's parameters type name.
    /// </summary>
    public string ParamsTypeName { get; set; }

    /// <summary>
    /// The job's state type name.
    /// </summary>
    public string StateTypeName { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
