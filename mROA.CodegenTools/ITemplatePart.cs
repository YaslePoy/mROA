namespace mROA.CodegenTools
{
    public interface ITemplatePart
    {
        TemplateDocument Context { get; set; }
        string Bake();
    }

    public interface ITagged : ITemplatePart
    {
        string Tag { get; }
    }
}