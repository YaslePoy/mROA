using System.Linq;

namespace mROA.CodegenTools
{
    public class LinkTemplateSection : ITemplateSection, IBaking
    {
        private readonly string _linkedTag;

        public LinkTemplateSection(string linkedTag, TemplateDocument context)
        {
            _linkedTag = linkedTag;
            Context = context;
        }

        public TemplateDocument Context { get; set; }

        public string LinkedTag => _linkedTag;

        public int TargetLength
        {
            get
            {
                var attached = Context[_linkedTag]; 
                return attached?.TargetLength ?? 0;
            }
        }

        public string Bake()
        {
            return Context[_linkedTag]?.ToString();
        }

        public override string ToString()
        {
            return $"{nameof(LinkedTag)}: {_linkedTag}";
        }
    }
}