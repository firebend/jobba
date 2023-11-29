using System;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Jobba.Store.Mongo.Serializers;

public class TypeSerializer : SerializerBase<Type>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
        => context.Writer.WriteString(value.AssemblyQualifiedName);

    public override Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => Type.GetType(context.Reader.ReadString());
}
