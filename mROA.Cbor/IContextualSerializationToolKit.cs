using System;
using mROA.Abstract;

namespace mROA.Cbor
{
    public interface IContextualSerializationToolKit
    {
        byte[] Serialize<T>(T objectToSerialize, IEndPointContext context);
        byte[] Serialize(object objectToSerialize, Type type, IEndPointContext context);
        T Deserialize<T>(byte[] rawData, IEndPointContext context);
        object? Deserialize(byte[] rawData, Type type, IEndPointContext context);
        T Deserialize<T>(Span<byte> rawData, IEndPointContext context);
        object? Deserialize(Span<byte> rawData, Type type, IEndPointContext context);
        T Cast<T>(object nonCasted, IEndPointContext context);
        object Cast(object nonCasted, Type type, IEndPointContext context);
    }
}