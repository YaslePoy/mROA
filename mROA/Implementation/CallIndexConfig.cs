using System.Collections.Generic;

namespace mROA.Implementation
{
    public class CallIndexConfig : List<IndexSpan>
    {
    }

    public class IndexSpan
    {
        public string Identifier { get; set; }
        public int ApiLevel { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }
}