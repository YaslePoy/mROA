using System.Linq;

namespace mROA.CodegenTools.Reading
{
    public class InsertSectionReader : LeadingTextSectionReader
    {
        public InsertSectionReader() : base("<!I")
        {
        }


        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var from = index;
            index = PassCaretToCloseSymbol();
            var to = index - 1;

            var tagText = GetTagValue(from, to);
            var parts = tagText.Split(' ');

            return new InsertTemplateSection(parts[0], document, parts.Skip(1).ToArray());
        }
    }
}