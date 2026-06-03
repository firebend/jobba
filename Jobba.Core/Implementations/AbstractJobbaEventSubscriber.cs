using System;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations;

public abstract class AbstractJobbaEventSubscriber
{
    private readonly string _systemMoniker;

    protected AbstractJobbaEventSubscriber(IJobSystemInfoProvider systemInfoProvider)
    {
        _systemMoniker = systemInfoProvider.GetSystemInfo()?.SystemMoniker ?? string.Empty;
    }

    protected bool ShouldProcessEvent(IJobbaEvent @event)
    {
        // in case we somehow get an event for a different system, we need to ignore it
        var eventMoniker = @event.SystemMoniker ?? string.Empty;
        return string.Equals(eventMoniker, _systemMoniker, StringComparison.Ordinal);
    }
}
