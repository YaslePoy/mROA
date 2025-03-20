namespace mROA.CodegenTools
{
    public interface ITemplateSection
    {
        TemplateDocument Context { get; set; }
        int TargetLength { get; }
        string Bake();
    }

    public interface ITagged : ITemplateSection
    {
        string Tag { get; }
    }
}