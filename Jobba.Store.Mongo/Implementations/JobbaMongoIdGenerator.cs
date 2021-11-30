using System;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using MongoDB.Bson.Serialization;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoIdGenerator : IIdGenerator
    {
        private readonly IJobbaGuidGenerator _guidGenerator;

        public JobbaMongoIdGenerator(IJobbaGuidGenerator guidGenerator)
        {
            _guidGenerator = guidGenerator;
        }

        public object GenerateId(object container, object document)
        {
            var guid = _guidGenerator.GenerateGuidAsync(default).GetAwaiter().GetResult();
            return guid;
        }

        public bool IsEmpty(object id) => id == default || (Guid)id == Guid.Empty;

        public static readonly JobbaMongoIdGenerator Instance = new JobbaMongoIdGenerator(new DefaultJobbaGuidGenerator());
    }
}
