using System;

namespace mROA.Implementation.Attributes
{
    public class ApiLevelAttribute : Attribute
    {
        public int ApiLevel { get; set; }

        public ApiLevelAttribute(int level)
        {
            ApiLevel = level;
        }

        public ApiLevelAttribute() : this(0)
        {
        }
    }
}