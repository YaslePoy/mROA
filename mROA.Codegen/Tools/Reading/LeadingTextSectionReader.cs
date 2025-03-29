using System;

namespace mROA.Codegen.Tools.Reading
{
    public abstract class LeadingTextSectionReader : ISectionReader
    {
        private readonly string _tagLeading;
        protected int _currentCaretPosition;

        protected LeadingTextSectionReader(string tagLeading)
        {
            _tagLeading = tagLeading + " ";
        }

        public string TemplateText { get; set; }

        public int GetNextSectionIndex(int currentIndex)
        {
            _currentCaretPosition = Math.Max(currentIndex, _currentCaretPosition);
            var index = TemplateText.IndexOf(_tagLeading, _currentCaretPosition, StringComparison.Ordinal);
            _currentCaretPosition = index;
            return index;
        }

        public abstract ITemplateSection ExtractSection(ref int index, TemplateDocument document);

        protected int PassCaretToCloseSymbol()
        {
            _currentCaretPosition = TemplateText.IndexOf(">", _currentCaretPosition, StringComparison.Ordinal) + 1;
            return _currentCaretPosition;
        }

        protected string GetTagValue(int from, int to)
        {
            var realStart = from + 4;
            var span = TemplateText.AsSpan();
            var slice = span.Slice(realStart, to - realStart);
            return new string(slice.ToArray());
        }
    }
}