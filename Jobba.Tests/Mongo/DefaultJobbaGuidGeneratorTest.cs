using System.Threading.Tasks;
using FluentAssertions;
using Jobba.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobba.Tests.Mongo;

[TestClass]
public class DefaultJobbaGuidGeneratorTest
{
    [TestMethod]
    public async Task Default_Jobba_Guid_Generator_Should_Generate_Guid()
    {
        //arrange
        var generator = new DefaultJobbaGuidGenerator();

        //act
        var guid = await generator.GenerateGuidAsync(default);

        //assert
        guid.Should().NotBeEmpty();
    }
}
