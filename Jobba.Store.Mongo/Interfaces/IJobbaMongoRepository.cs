using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Interfaces;

public interface IJobbaMongoRepository<TEntity>
    where TEntity : class, IJobbaEntity
{
    public Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken);

    public Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken);

    public Task<TEntity> UpdateAsync(Guid id, UpdateDefinition<TEntity> update, CancellationToken cancellationToken);

    public Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken);

    public Task<List<TEntity>> DeleteManyAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken);

    public Task<TEntity> UpsertAsync(Expression<Func<TEntity, bool>> expression, TEntity entity, CancellationToken cancellationToken);
}
