using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Tests;

public class TestModels
{
    public static readonly JobSystemInfo TestSystemInfo =
        new JobSystemInfo("TestMoniker", "TestComputerName", "TestUser", "TestOperationSystem");

    public class FooState : IJobState
    {
        public string Bar { get; set; }
    }

    public class FooParams : IJobParams
    {
        public string Baz { get; set; }
    }

    public class FooJob : AbstractJobBaseClass<FooParams, FooState>
    {
        public FooJob(IJobProgressStore progressStore) : base(progressStore)
        {
        }

        public override string JobName => "Jerb";

        protected override Task OnStartAsync(JobStartContext<FooParams, FooState> jobStartContext,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
