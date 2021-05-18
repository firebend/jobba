using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Jobba.Core.Interfaces;
using Jobba.Store.Mongo.Implementations;
using Jobba.Store.Mongo.Interfaces;
using Jobba.Store.Mongo.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Moq;

namespace Jobba.Tests.Mongo
{
    [TestClass]
    public class JobbaMongoJobStoreTests
    {
        public class Foo : IJobbaEntity
        {
            public Guid Id { get; set; }
            public string Fake { get; set; }
        }

        private static IFixture SetUpFixture()
        {
            var fixture = new Fixture();

            var mockCollection = fixture.Freeze<Mock<IMongoCollection<Foo>>>();
            var mockDatabase = fixture.Freeze<Mock<IMongoDatabase>>();
            var mockClient = fixture.Freeze<Mock<IMongoClient>>();
            var mockEntityConfiguration = fixture.Freeze<Mock<IJobbaEntityConfigurationProvider<Foo>>>();
            var mockGuidGenerator = fixture.Freeze<Mock<IJobbaGuidGenerator>>();

            mockDatabase.Setup(d => d.GetCollection<Foo>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>())).Returns(mockCollection.Object);

            mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>())).Returns(mockDatabase.Object);

            mockEntityConfiguration.Setup(x => x.GetConfiguration())
                .Returns(new JobbaEntityConfiguration {Collection = "fake", Database = "af"});

            mockGuidGenerator.Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            fixture.Customize(new AutoMoqCustomization());

            return fixture;
        }

        [TestMethod]
        public async Task Jobba_Mongo_Job_Store_Should_Add()
        {
            //arrange
            var fixture = SetUpFixture();
            var foo = fixture.Create<Foo>();

            var service = fixture.Create<JobbaMongoJobStore<Foo>>();

            //act
            var added = await service.AddAsync(foo, default);

            //assert
            added.Should().NotBeNull();
            added.Should().BeEquivalentTo(added);
        }

        [TestMethod]
        public async Task Jobba_Mongo_Job_Store_Should_Add_And_Set_Id()
        {
            //arrange
            var fixture = SetUpFixture();
            var foo = fixture.Create<Foo>();
            foo.Id = Guid.Empty;

            var service = fixture.Create<JobbaMongoJobStore<Foo>>();

            //act
            var added = await service.AddAsync(foo, default);

            //assert
            added.Should().NotBeNull();
            added.Id.Should().NotBeEmpty();
        }
    }
}
