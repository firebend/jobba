namespace Jobba.Core.Interfaces;

public record JobSystemInfo(string SystemMoniker,
    string ComputerName,
    string User,
    string OperatingSystem)
{
    public override string ToString()
        => $"System Moniker: {SystemMoniker} Computer Name: {ComputerName} User: {User} Operation System: {OperatingSystem}";
}

public interface IJobSystemInfoProvider
{
    JobSystemInfo GetSystemInfo();
}
