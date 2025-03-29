namespace mROA.Codegen.Tools
{
    public class InnerTemplateSection : ITemplateSection, ITagged
    {
        public InnerTemplateSection(string tag, TemplateDocument innerTemplate, TemplateDocument context)
        {
            Tag = tag;
            Context = context;
            InnerTemplate = innerTemplate;
        }

        public TemplateDocument InnerTemplate { get; }

        public string Tag { get; }

        public TemplateDocument Context { get; set; }
        public int TargetLength => 0;

        public object Clone()
        {
            return new InnerTemplateSection(Tag, InnerTemplate, Context);
        }
    }
}