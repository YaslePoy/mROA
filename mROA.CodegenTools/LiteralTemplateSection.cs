namespace mROA.CodegenTools
{
    public class LiteralTemplateSection : ITemplateSection, IBaking
    {
        private string _internalText;

        public LiteralTemplateSection(string internalText, TemplateDocument context)
        {
            _internalText = internalText;
            Context = context;
        }

        public TemplateDocument Context { get; set; }
        public int TargetLength => _internalText.Length;

        public string Bake()
        {
            return ToString();
        }

        public override string ToString()
        {
            return _internalText;
        }
    }
}