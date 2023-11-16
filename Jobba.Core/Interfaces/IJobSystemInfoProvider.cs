namespace Jobba.Core.Interfaces;

public record JobSystemInfo(string SystemMoniker,
    string ComputerName,
    string User,
    string OperatingSystem);

public interface IJobSystemInfoProvider
{
    JobSystemInfo GetSystemInfo();
}
