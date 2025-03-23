namespace mROA.CodegenTools
{
    public class InnerTemplateSection : ITemplateSection, ITagged
    {
        private readonly string _tag;
        public TemplateDocument Context { get; set; }
        public int TargetLength => 0;

        public string Tag => _tag;
        
        public TemplateDocument InnerTemplate { get; }

        public InnerTemplateSection(string tag, TemplateDocument innerTemplate, TemplateDocument context)
        {
            _tag = tag;
            Context = context;
            InnerTemplate = innerTemplate;
        }
    }
}