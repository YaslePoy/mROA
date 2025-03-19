namespace mROA.CodegenTools
{
    public class LiteralTemplatePart : ITemplatePart
    {
        private string _internalText;

        public LiteralTemplatePart(string internalText, TemplateDocument context)
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