using System.Collections.Generic;
using Jobba.MassTransit.Models;

namespace Jobba.MassTransit.Interfaces
{
    public interface IJobbaMassTransitConsumerInfoProvider
    {
        IEnumerable<JobbaMassTransitConsumerInfo> GetConsumerInfos();
    }
}
