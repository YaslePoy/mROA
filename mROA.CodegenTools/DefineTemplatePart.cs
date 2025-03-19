using System;

namespace mROA.CodegenTools
{
    public class DefineTemplatePart : ITemplatePart, ITagged
    {
        public TemplateDocument Context { get; set; }
        public string StoredText { get; }
        private string _tag;

        public DefineTemplatePart(string storedText)
        {
            StoredText = storedText;
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