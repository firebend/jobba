using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Store.Mongo.Abstractions;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoRepository<TEntity> : JobbaMongoEntityClient<TEntity>, IJobbaMongoRepository<TEntity>
    where TEntity : class, IJobbaEntity
{
    private readonly IJobbaGuidGenerator _guidGenerator;

    public JobbaMongoRepository(IMongoClient client,
        ILogger<JobbaMongoRepository<TEntity>> logger,
        IJobbaEntityConfigurationProvider<TEntity> entityConfigurationProvider,
        IJobbaGuidGenerator guidGenerator,
        IJobbaMongoRetryService retryService) : base(client, logger, entityConfigurationProvider, retryService)
    {
        _guidGenerator = guidGenerator;
    }

    private async Task AssignGuidAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = await _guidGenerator.GenerateGuidAsync(cancellationToken);
        }
    }

    public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
    {
        var asyncCursor = await FilterCollection(filter, cancellationToken);
        var list = await asyncCursor.ToListAsync(cancellationToken);
        return list;
    }

    public async Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
    {
        var asyncCursor = await FilterCollection(filter, cancellationToken);
        var entity = await asyncCursor.FirstOrDefaultAsync(cancellationToken);
        return entity;
    }

    public Task<TEntity> UpdateAsync(Guid id, UpdateDefinition<TEntity> update, CancellationToken cancellationToken)
        => RetryErrorAsync(() => GetCollection()
            .FindOneAndUpdateAsync(
                x => x.Id == id,
                update,
                new() { ReturnDocument = ReturnDocument.After },
                cancellationToken));

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await AssignGuidAsync(entity, cancellationToken);

        await RetryErrorAsync(() => GetCollection().InsertOneAsync(entity, new InsertOneOptions(), cancellationToken));

        return entity;
    }

    public async Task<List<TEntity>> DeleteManyAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
    {
        var found = await GetAllAsync(filter, cancellationToken);

        foreach (var entity in found)
        {
            await RetryErrorAsync(() => GetCollection().DeleteOneAsync(x => x.Id == entity.Id, cancellationToken));
        }

        return found;
    }

    public async Task<TEntity> UpsertAsync(Expression<Func<TEntity, bool>> expression, TEntity entity, CancellationToken cancellationToken)
    {
        var options = new FindOneAndReplaceOptions<TEntity>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true,
        };

        var filterDef = Builders<TEntity>.Filter.Where(expression);

        var result = await RetryErrorAsync(() => GetCollection().FindOneAndReplaceAsync(filterDef, entity, options, cancellationToken));
        return result;
    }

    protected Task<IAsyncCursor<TEntity>> FilterCollection(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken) =>
        RetryErrorAsync(() => GetCollection().FindAsync(Builders<TEntity>.Filter.Where(filter), new FindOptions<TEntity>(), cancellationToken));
}
