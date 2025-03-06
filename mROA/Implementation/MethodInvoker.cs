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
        public Func<object, object?[]?, object[], object?> Invoking { get; set; } = (_, _, _) => null; 

        public object? Invoke(object instance, object?[]? parameters, object[] special)
        {
            return Invoking(instance, parameters, special);
        }

        public Type SuitableType { get; set; } = null!;
        
        public static readonly IMethodInvoker Dispose = new MethodInvoker
        {
            IsAsync = false,
            IsVoid = true,
            ReturnType = null,
            Invoking = (instance, _, _) =>
            {
                (instance as IDisposable)?.Dispose();
                return null;
            },
            SuitableType = typeof(IDisposable)
        };
    }
}