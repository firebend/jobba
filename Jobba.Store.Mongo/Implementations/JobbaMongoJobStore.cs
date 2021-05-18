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
using MongoDB.Driver.Linq;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobStore<TEntity> : JobbaMongoEntityClient<TEntity>,  IMongoJobStore<TEntity>
        where TEntity : class, IJobbaEntity
    {
        private readonly IJobbaGuidGenerator _guidGenerator;

        public JobbaMongoJobStore(IMongoClient client,
            ILogger<JobbaMongoJobStore<TEntity>> logger,
            IJobbaEntityConfigurationProvider<TEntity> entityConfigurationProvider,
            IJobbaGuidGenerator guidGenerator) : base(client, logger, entityConfigurationProvider)
        {
            _guidGenerator = guidGenerator;
        }

        public Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
            => RetryErrorAsync(() => GetCollection().AsQueryable().Where(filter).ToListAsync(cancellationToken));

        public Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
            => RetryErrorAsync(() => GetCollection().AsQueryable().Where(filter).FirstOrDefaultAsync(cancellationToken));

        public async Task<TEntity> UpdateAsync(Guid id, JsonPatchDocument<TEntity> patch, CancellationToken cancellationToken = default)
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
