namespace mROA.CodegenTools
{
    public class LiteralTemplatePart : ITemplatePart
    {
        private string _internalText;
        public TemplateDocument Context { get; set; }
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