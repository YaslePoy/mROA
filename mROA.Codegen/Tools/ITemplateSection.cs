using System;

namespace mROA.Codegen.Tools
{
    public interface ITemplateSection : ICloneable
    {
        TemplateDocument Context { get; set; }
        int TargetLength { get; }
    }

    public interface IBaking
    {
        string Bake();
    }

    public interface ITagged : ITemplateSection
    {
        string Tag { get; }
    }
}