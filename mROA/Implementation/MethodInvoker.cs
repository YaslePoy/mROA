using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class MethodInvoker : IMethodInvoker
    {
        public bool IsVoid { get; set; }
        public bool IsTrusted { get; set; } = true;
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
            IsVoid = true,
            Invoking = (instance, _, _) =>
            {
                (instance as IDisposable)?.Dispose();
                return null;
            },
            SuitableType = typeof(IDisposable)
        };
    }

    public class AsyncMethodInvoker : IMethodInvoker
    {
        public bool IsVoid { get; set; }
        public bool IsTrusted { get; set; } = true;
        public Type[] ParameterTypes { get; set; } = Type.EmptyTypes;
        public Type? ReturnType { get; set; }
        public Type SuitableType { get; set; } = typeof(object);

        public Action<object, object?[]?, object[], Action<object?>> Invoking { get; set; } =
            (_, _, _, post) => { post.Invoke(null); };

        public void Invoke(object instance, object?[]? parameters, object[] special, Action<object?> postInvokeAction)
        {
            Invoking(instance, parameters, special, postInvokeAction);
        }
    }
}