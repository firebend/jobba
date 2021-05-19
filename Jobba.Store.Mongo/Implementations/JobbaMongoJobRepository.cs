using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Store.Mongo.Abstractions;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobRepository<TEntity> : JobbaMongoEntityClient<TEntity>,  IMongoJobRepository<TEntity>
        where TEntity : class, IJobbaEntity
    {
        private readonly IJobbaGuidGenerator _guidGenerator;

        public JobbaMongoJobRepository(IMongoClient client,
            ILogger<JobbaMongoJobRepository<TEntity>> logger,
            IJobbaEntityConfigurationProvider<TEntity> entityConfigurationProvider,
            IJobbaGuidGenerator guidGenerator) : base(client, logger, entityConfigurationProvider)
        {
            _guidGenerator = guidGenerator;
        }

        protected Task<IAsyncCursor<TEntity>> FilterCollection(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken) =>
            RetryErrorAsync(() => GetCollection().FindAsync(Builders<TEntity>.Filter.Where(filter), new FindOptions<TEntity>(), cancellationToken));

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

        public async Task<TEntity> UpdateAsync(Guid id, JsonPatchDocument<TEntity> patch, CancellationToken cancellationToken)
        {
            var entity = await GetFirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
            {
                return null;
            }

            patch.ApplyTo(entity);

            var options = new FindOneAndReplaceOptions<TEntity> {ReturnDocument = ReturnDocument.After, IsUpsert = true};
            var filterDef = Builders<TEntity>.Filter.Where(x => x.Id == id);

            var result = await RetryErrorAsync(() => GetCollection().FindOneAndReplaceAsync(filterDef, entity, options, cancellationToken));
            return result;
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = await _guidGenerator.GenerateGuidAsync(cancellationToken);
            }

            await RetryErrorAsync(() => GetCollection().InsertOneAsync(entity, new InsertOneOptions(), cancellationToken));

            return entity;
        }
    }
}
