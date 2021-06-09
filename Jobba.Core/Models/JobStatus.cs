namespace Jobba.Core.Models
{
    public enum JobStatus
    {
        Unknown = 0,
        Faulted = 1,
        Completed = 2,
        InProgress = 3,
        Enqueued = 4,
        Cancelled = 5,
    }
}
