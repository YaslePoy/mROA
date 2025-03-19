namespace mROA.CodegenTools
{
    public interface ITemplatePart
    {
        TemplateDocument Context { get; set; }
        int TargetLength { get; }
        string Bake();
    }

    public interface ITagged : ITemplatePart
    {
        string Tag { get; }
    }
}