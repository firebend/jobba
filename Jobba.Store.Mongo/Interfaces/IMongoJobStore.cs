using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Microsoft.AspNetCore.JsonPatch;

namespace Jobba.Store.Mongo.Interfaces
{
    public interface IMongoJobStore<TEntity>
        where TEntity : class, IJobbaEntity
    {
        Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken);

        Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken);

        Task<TEntity> UpdateAsync(Guid id, JsonPatchDocument<TEntity> patch, CancellationToken cancellationToken);

        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken);
    }
}
