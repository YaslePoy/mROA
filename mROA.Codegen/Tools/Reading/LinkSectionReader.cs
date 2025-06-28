namespace mROA.CodegenTools.Reading
{
    public class LinkSectionReader : LeadingTextSectionReader
    {
        public LinkSectionReader() : base("<!L")
        {
        }


        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var from = index;
            index = PassCaretToCloseSymbol();
            var to = index - 1;

            var tagText = GetTagValue(from, to);
            return new LinkTemplateSection(tagText, document);
        }
    }
}