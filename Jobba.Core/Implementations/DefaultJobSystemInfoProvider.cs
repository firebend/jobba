using System;
using Jobba.Core.Interfaces;

namespace Jobba.Core.Implementations;

public class DefaultJobSystemInfoProvider : IJobSystemInfoProvider
{
    private readonly JobSystemInfo _info;

    public DefaultJobSystemInfoProvider(string moniker)
    {
        _info = new(moniker,
            Environment.MachineName,
            Environment.UserDomainName,
            Environment.OSVersion.VersionString);
    }

    public JobSystemInfo GetSystemInfo() => _info;
}
