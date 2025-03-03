using System;

namespace mROA.Abstract
{
    public interface IMethodInvoker
    {
        bool IsAsync { get; }
        bool IsVoid { get; }
        Type[] ParameterTypes { get; }
        Type? ReturnType { get; }
        object? Invoke(object instance, object?[] parameters, object[] special);
    }
}