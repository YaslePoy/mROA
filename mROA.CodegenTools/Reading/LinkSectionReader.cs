namespace mROA.CodegenTools
{
    public class LinkSectionReader : LeadingTextSectionReader
    {
        public LinkSectionReader() : base("<!L")
        {
        }


        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var from = index;
            var to = PassCaretToCloseSymbol();
            index = to;

            var tagText = GetTagValue(from, to);
            return new LinkTemplateSection(tagText, document);
        }
    }
}