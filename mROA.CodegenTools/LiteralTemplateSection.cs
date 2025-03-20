namespace mROA.CodegenTools
{
    public class LiteralTemplateSection : ITemplateSection
    {
        private string _internalText;

        public LiteralTemplateSection(string internalText, TemplateDocument context)
        {
            _internalText = internalText;
            Context = context;
        }

        public TemplateDocument Context { get; set; }
        public int TargetLength { get; }

        public string Bake()
        {
            return _internalText;
        }

        public override string ToString()
        {
            return _internalText;
        }
    }
}