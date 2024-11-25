namespace Jobba.Core.Interfaces;

public record JobSystemInfo
{
    public JobSystemInfo()
    {
    }

    public JobSystemInfo(string systemMoniker,
        string computerName,
        string user,
        string operatingSystem)
    {
        SystemMoniker = systemMoniker;
        ComputerName = computerName;
        User = user;
        OperatingSystem = operatingSystem;
    }

    public string SystemMoniker { get; init; }
    public string ComputerName { get; init; }
    public string User { get; init; }
    public string OperatingSystem { get; init; }

    public override string ToString()
        => $"System Moniker: {SystemMoniker} Computer Name: {ComputerName} User: {User} Operation System: {OperatingSystem}";
}

public interface IJobSystemInfoProvider
{
    JobSystemInfo GetSystemInfo();
}
