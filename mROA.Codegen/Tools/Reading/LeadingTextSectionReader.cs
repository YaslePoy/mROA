using System;

namespace mROA.CodegenTools
{
    public abstract class LeadingTextSectionReader : ISectionReader
    {
        protected int _currentCaretPosition;
        private string _tagLeading;

        protected LeadingTextSectionReader(string tagLeading)
        {
            _tagLeading = tagLeading + " ";
        }

        protected int PassCaretToCloseSymbol()
        {
            _currentCaretPosition = TemplateText.IndexOf(">", _currentCaretPosition, StringComparison.Ordinal) + 1;
            return _currentCaretPosition;
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

        protected string GetTagValue(int from, int to)
        {
            var realStart = from + 4;
            var span = TemplateText.AsSpan();
            var slice = span.Slice(realStart, to - realStart);
            return new string(slice.ToArray());
        }
    }
}