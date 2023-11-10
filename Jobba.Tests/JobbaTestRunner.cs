using Jobba.Store.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests;

[TestClass]
public class JobbaTestRunner
{
    [AssemblyInitialize]
    public static void InitAssembly(TestContext testContext)
    {
        JobbaMongoDbConfigurator.Configure();
    }
}
