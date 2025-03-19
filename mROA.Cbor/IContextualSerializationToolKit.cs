﻿using System;
using mROA.Abstract;

namespace mROA.Cbor
{
    public interface IContextualSerializationToolKit : ISerializationToolkit
    {
        byte[] Serialize(object objectToSerialize, IEndPointContext? context);
        void Serialize(object objectToSerialize, Span<byte> destination, IEndPointContext? context);
        T Deserialize<T>(byte[] rawData, IEndPointContext? context);
        object? Deserialize(byte[] rawData, Type type, IEndPointContext? context);
        T Deserialize<T>(ReadOnlyMemory<byte> rawMemory, IEndPointContext? context);
        object? Deserialize(ReadOnlyMemory<byte> rawMemory, Type type, IEndPointContext? context);
        T Cast<T>(object nonCasted, IEndPointContext? context);
        object? Cast(object nonCasted, Type type, IEndPointContext? context);
    }
}