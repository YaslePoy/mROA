using System.Collections.Generic;

namespace mROA.CodegenTools
{
    public interface ISectionReader
    {
        string TemplateText { get; set; }
        int GetNextSectionIndex(int currentIndex);
        ITemplateSection ExtractSection(ref int index, TemplateDocument document);
    }
}