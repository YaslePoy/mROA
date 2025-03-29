namespace mROA.Codegen.Tools.Reading
{
    public interface ISectionReader
    {
        string TemplateText { get; set; }
        int GetNextSectionIndex(int currentIndex);
        ITemplateSection ExtractSection(ref int index, TemplateDocument document);
    }
}