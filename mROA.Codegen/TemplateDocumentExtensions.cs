using System;
using mROA.CodegenTools;

namespace mROA.Codegen
{
    public static class TemplateDocumentExtensions
    {
        public static TemplateDocument CloneInnerTemplate(this TemplateDocument template, string tag)
        {
            var innerTemplate = template[tag] as InnerTemplateSection 
                                ?? throw new InvalidOperationException();
            var clone = innerTemplate.InnerTemplate.Clone();
            return (TemplateDocument)clone;
        }
    }
}