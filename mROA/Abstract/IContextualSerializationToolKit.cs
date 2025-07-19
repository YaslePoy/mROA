using System;

namespace mROA.Abstract
{
    public interface IContextualSerializationToolKit
    {
        byte[] Serialize(object objectToSerialize, IEndPointContext? context);
        int Serialize(object objectToSerialize, Span<byte> destination, IEndPointContext? context);
        T Deserialize<T>(byte[] rawData, IEndPointContext? context);
        object? Deserialize(byte[] rawData, Type type, IEndPointContext? context);
        T Deserialize<T>(ReadOnlyMemory<byte> rawMemory, IEndPointContext? context);
        object? Cast(object? nonCasted, Type type, IEndPointContext? context);
    }
}