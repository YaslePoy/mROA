using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class MethodInvoker : IMethodInvoker
    {
        public bool IsAsync { get; set; }
        public bool IsVoid { get; set; }
        public Type[] ParameterTypes { get; set; } = Type.EmptyTypes;
        public Type? ReturnType { get; set; }
        public Func<object?, object, object?[], object[]> Invoking { get; set; }

        public object? Invoke(object instance, object?[] parameters, object[] special)
        {
            return Invoking(instance, parameters, special);
        }
    }
}