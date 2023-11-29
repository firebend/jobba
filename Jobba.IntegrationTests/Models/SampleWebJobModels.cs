using Jobba.Core.Interfaces;

namespace Jobba.IntegrationTests.Models;

public class SampleWebJobState : IJobState
{
    public int Tries { get; set; }
}

public class SampleWebJobParameters : IJobParams
{
    public string Greeting { get; set; }
}
