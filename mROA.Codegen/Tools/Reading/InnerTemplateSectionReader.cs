using System;

namespace mROA.Codegen.Tools.Reading
{
    public class InnerTemplateSectionReader : LeadingTextSectionReader
    {
        public InnerTemplateSectionReader()
            : base("<!T")
        {
        }


        public override ITemplateSection ExtractSection(ref int index, TemplateDocument document)
        {
            var end = FindEnd(index + 4);
            var from = index;
            index = PassCaretToCloseSymbol();
            var to = index - 1;

            var tagName = GetTagValue(from, to);

            var innerDocText = TemplateText.Substring(index, end - index);
            var innerDoc = TemplateReader.Parse(innerDocText);
            _currentCaretPosition = end + 4;
            index = _currentCaretPosition;
            return new InnerTemplateSection(tagName, innerDoc, document);
        }

        private int FindEnd(int start)
        {
            var endSymbol = TemplateText.IndexOf("<!T>", start, StringComparison.Ordinal);
            var innerStartSymbol = TemplateText.IndexOf("<!T ", start, StringComparison.Ordinal);
            if (endSymbol < innerStartSymbol || innerStartSymbol == -1)
                return endSymbol;

            return FindEnd(endSymbol + 4);
        }
    }
}