using System;

namespace mROA.Abstract
{
    public interface IMethodInvoker
    {
        bool IsVoid { get; }
        Type[] ParameterTypes { get; }
        Type? ReturnType { get; }
        Type SuitableType { get; }
    }
}