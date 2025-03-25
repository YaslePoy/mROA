using System;

namespace mROA.CodegenTools
{
    public class DefineTemplateSection : ITagged
    {
        public TemplateDocument Context { get; set; }
        public string StoredText { get; }
        private readonly string _tag;

        public DefineTemplateSection(string storedText, string tag, TemplateDocument context)
        {
            StoredText = storedText;
            _tag = tag;
            Context = context;
        }

        public string Tag => _tag;
        public int TargetLength => StoredText.Length;


        public override string ToString()
        {
            return StoredText;
        }

        public object Clone()
        {
            return new DefineTemplateSection(StoredText, _tag, Context);
        }
    }
}