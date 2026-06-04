using System;

namespace Jobba.Core.Events;

public interface IJobbaEvent
{
    public Guid JobId { get; set; }
    public Guid JobRegistrationId { get; set; }
    public string SystemMoniker { get; set; }
}
