using System.Linq;

namespace mROA.CodegenTools
{
    public class InsertSectionReader : LeadingTextSectionReader
    {
        public InsertSectionReader() : base("<!I")
        {
        }


        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var from = index;
            var to = PassCaretToCloseSymbol();
            index = to;

            var tagText = GetTagValue(from, to);
            var parts = tagText.Split(' ');
            
            return new InsertTemplatePart(parts[0], document, parts.Skip(1).ToArray());
        }
    }
}