namespace mROA.CodegenTools.Reading
{
    public interface ISectionReader
    {
        string TemplateText { get; set; }
        int GetNextSectionIndex(int currentIndex);
        ITemplateSection ExtractSection(ref int index, TemplateDocument document);
    }
}