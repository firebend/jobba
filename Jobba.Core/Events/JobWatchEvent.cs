using System;

namespace Jobba.Core.Events;

public class JobWatchEvent
{
    public JobWatchEvent()
    {
    }

    public JobWatchEvent(Guid jobId, string paramsTypeName, string stateTypeName)
    {
        JobId = jobId;
        ParamsTypeName = paramsTypeName;
        StateTypeName = stateTypeName;
    }

    /// <summary>
    ///     The id of the job that needs to be watched.
    /// </summary>
    public Guid JobId { get; set; }

    public string ParamsTypeName { get; set; }
    public string StateTypeName { get; set; }
}
