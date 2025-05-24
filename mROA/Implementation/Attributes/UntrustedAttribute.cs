using System;

namespace mROA.Implementation.Attributes
{
    public class UntrustedAttribute : Attribute
    {
        public bool UseAwait { get; private set; }
        public UntrustedAttribute(bool useAwait = true)
        {
            UseAwait = useAwait;
        }
    }
}