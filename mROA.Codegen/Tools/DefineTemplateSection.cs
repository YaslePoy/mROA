namespace mROA.Codegen.Tools
{
    public class DefineTemplateSection : ITagged
    {
        public DefineTemplateSection(string storedText, string tag, TemplateDocument context)
        {
            StoredText = storedText;
            Tag = tag;
            Context = context;
        }

        public string StoredText { get; }
        public TemplateDocument Context { get; set; }

        public string Tag { get; }

        public int TargetLength => StoredText.Length;

        public object Clone()
        {
            return new DefineTemplateSection(StoredText, Tag, Context);
        }


        public override string ToString()
        {
            return StoredText;
        }
    }
}