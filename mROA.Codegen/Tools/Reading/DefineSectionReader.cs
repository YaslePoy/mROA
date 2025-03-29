using System;

namespace mROA.Codegen.Tools.Reading
{
    public class DefineSectionReader : LeadingTextSectionReader
    {
        public DefineSectionReader() : base("<!D")
        {
        }

        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var tagEnd = TemplateText.IndexOf(">", index, StringComparison.Ordinal);
            var sectionName = GetTagValue(index, tagEnd);
            tagEnd += 1;
            var endIndex = TemplateText.IndexOf("<!D>", tagEnd, StringComparison.Ordinal);
            var storedText = TemplateText.Substring(tagEnd, endIndex - tagEnd);
            index = endIndex + 4;
            PassCaretToCloseSymbol();
            PassCaretToCloseSymbol();
            return new DefineTemplateSection(storedText, sectionName, document);
        }
    }
}