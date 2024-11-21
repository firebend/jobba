namespace Jobba.Core.Interfaces;

public record JobSystemInfo
{
    public JobSystemInfo()
    {
    }

    public JobSystemInfo(string SystemMoniker,
        string ComputerName,
        string User,
        string OperatingSystem)
    {
        this.SystemMoniker = SystemMoniker;
        this.ComputerName = ComputerName;
        this.User = User;
        this.OperatingSystem = OperatingSystem;
    }

    public string SystemMoniker { get; init; }
    public string ComputerName { get; init; }
    public string User { get; init; }
    public string OperatingSystem { get; init; }

    public override string ToString()
        => $"System Moniker: {SystemMoniker} Computer Name: {ComputerName} User: {User} Operation System: {OperatingSystem}";

    public void Deconstruct(out string SystemMoniker, out string ComputerName, out string User, out string OperatingSystem)
    {
        SystemMoniker = this.SystemMoniker;
        ComputerName = this.ComputerName;
        User = this.User;
        OperatingSystem = this.OperatingSystem;
    }
}

public interface IJobSystemInfoProvider
{
    JobSystemInfo GetSystemInfo();
}
