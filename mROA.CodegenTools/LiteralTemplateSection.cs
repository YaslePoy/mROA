namespace mROA.CodegenTools
{
    public class LiteralTemplateSection : ITemplateSection, IBaking
    {
        private readonly string _internalText;

        public LiteralTemplateSection(string internalText, TemplateDocument context)
        {
            _internalText = internalText;
            Context = context;
        }

        public string Bake()
        {
            return ToString();
        }

        public TemplateDocument Context { get; set; }
        public int TargetLength => _internalText.Length;

        public object Clone()
        {
            return new LiteralTemplateSection(_internalText, Context);
        }

        public override string ToString()
        {
            return _internalText;
        }
    }
}