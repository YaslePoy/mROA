namespace mROA.CodegenTools
{
    public class LinkTemplateSection : ITemplateSection, IBaking
    {
        public LinkTemplateSection(string linkedTag, TemplateDocument context)
        {
            LinkedTag = linkedTag;
            Context = context;
        }

        public string LinkedTag { get; }

        public string Bake()
        {
            return Context[LinkedTag]?.ToString();
        }

        public TemplateDocument Context { get; set; }

        public int TargetLength
        {
            get
            {
                var attached = Context[LinkedTag];
                return attached?.TargetLength ?? 0;
            }
        }

        public object Clone()
        {
            return new LinkTemplateSection(LinkedTag, Context);
        }

        public override string ToString()
        {
            return $"{nameof(LinkedTag)}: {LinkedTag}";
        }
    }
}