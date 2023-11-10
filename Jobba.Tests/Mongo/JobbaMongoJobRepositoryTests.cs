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

namespace Jobba.Tests.Mongo;

[TestClass]
public class JobbaMongoJobRepositoryTests
{
    private static (IFixture fixture, Mock<IMongoCollection<Foo>> mongoCollection) SetUpFixture(Foo[] foos = null)
    {
        foos ??= Array.Empty<Foo>();
        var fixture = new Fixture();

        var mockCollection = fixture.Freeze<Mock<IMongoCollection<Foo>>>();
        var mockDatabase = fixture.Freeze<Mock<IMongoDatabase>>();
        var mockClient = fixture.Freeze<Mock<IMongoClient>>();
        var mockEntityConfiguration = fixture.Freeze<Mock<IJobbaEntityConfigurationProvider<Foo>>>();
        var mockGuidGenerator = fixture.Freeze<Mock<IJobbaGuidGenerator>>();
        var mockAsyncCursor = new Mock<IAsyncCursor<Foo>>();

        mockDatabase.Setup(d => d.GetCollection<Foo>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>())).Returns(mockCollection.Object);

        mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>())).Returns(mockDatabase.Object);

        mockEntityConfiguration.Setup(x => x.GetConfiguration())
            .Returns(new JobbaEntityConfiguration
            {
                Collection = "fake",
                Database = "af"
            });

        mockGuidGenerator.Setup(x => x.GenerateGuidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var seq = mockAsyncCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()));

        for (var i = 0; i < foos.Length; i++)
        {
            seq.Returns(true);
        }

        seq.Returns(false);

        var seqAsync = mockAsyncCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()));

        for (var i = 0; i < foos.Length; i++)
        {
            seqAsync.ReturnsAsync(true);
        }

        seqAsync.ReturnsAsync(false);

        mockAsyncCursor.SetupGet(x => x.Current).Returns(foos);

        mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Foo>>(),
                It.IsAny<FindOptions<Foo>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAsyncCursor.Object);

        fixture.Customize(new AutoMoqCustomization());

        return (fixture, mockCollection);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Repository_Should_Add()
    {
        //arrange
        var (fixture, _) = SetUpFixture();
        var foo = fixture.Create<Foo>();

        var service = fixture.Create<JobbaMongoRepository<Foo>>();

        //act
        var added = await service.AddAsync(foo, default);

        //assert
        added.Should().NotBeNull();
        added.Should().BeEquivalentTo(added);
    }

    [TestMethod]
    public async Task Jobba_Mongo_Repository_Should_Add_And_Set_Id()
    {
        //arrange
        var (fixture, _) = SetUpFixture();
        var foo = fixture.Create<Foo>();
        foo.Id = Guid.Empty;

        var service = fixture.Create<JobbaMongoRepository<Foo>>();

        //act
        var added = await service.AddAsync(foo, default);

        //assert
        added.Should().NotBeNull();
        added.Id.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Jobba_Mongo_Repository_Should_Update()
    {
        //arrange
        var id = Guid.NewGuid();
        var foo = new Foo
        {
            Id = id,
            Fake = "you's real af"
        };
        var (fixture, collection) = SetUpFixture(new[]
        {
            foo
        });

        var update = Builders<Foo>.Update.Set(x => x.Fake, "you's fake af");
        fixture.Register<IJobbaMongoRetryService>(() => new JobbaMongoRetryService());

        collection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Foo>>(),
                It.IsAny<UpdateDefinition<Foo>>(),
                It.IsAny<FindOneAndUpdateOptions<Foo, Foo>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(foo);

        var service = fixture.Create<JobbaMongoRepository<Foo>>();

        //act
        var updated = await service.UpdateAsync(id, update, default);

        //assert
        updated.Should().NotBeNull();
        updated.Id.Should().NotBeEmpty();
    }

    public class Foo : IJobbaEntity
    {
        public string Fake { get; set; }
        public Guid Id { get; set; }
    }
}
