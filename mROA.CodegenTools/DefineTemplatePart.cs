using System;

namespace mROA.CodegenTools
{
    public class DefineTemplatePart : ITemplatePart, ITagged
    {
        public TemplateDocument Context { get; set; }
        public string StoredText { get; }
        private readonly string _tag;

        public DefineTemplatePart(string storedText, string tag, TemplateDocument context)
        {
            StoredText = storedText;
            _tag = tag;
            Context = context;
        }

        public string Tag => _tag;
        public string Bake()
        {
            return String.Empty;
        }

        public override string ToString()
        {
            return StoredText;
        }
    }
}