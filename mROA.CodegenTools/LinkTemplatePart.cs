using System.Linq;

namespace mROA.CodegenTools
{
    public class LinkTemplatePart : ITemplatePart
    {
        private readonly string _linkedTag;

        public LinkTemplatePart(string linkedTag, TemplateDocument context)
        {
            _linkedTag = linkedTag;
            Context = context;
        }

        public TemplateDocument Context { get; set; }

        public string LinkedTag => _linkedTag;

        public int TargetLength { get; }

        public string Bake()
        {
            return Context[_linkedTag]?.Bake();
        }
    }
}