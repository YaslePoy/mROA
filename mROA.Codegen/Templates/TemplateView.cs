using mROA.CodegenTools;
using mROA.CodegenTools.Reading;

namespace mROA.Codegen.Templates
{
    public abstract class TemplateView
    {
        private readonly TemplateDocument _template;
        
        protected TemplateView(string templateFile)
        {
            _template = TemplateReader.FromEmbeddedResource(templateFile);
        }
        
        protected TemplateView(TemplateDocument template)
        {
            _template = template;
        }

        public string Compile()
        {
            return _template.Compile();
        }

        protected void Define(string tag, string value)
        {
            _template.AddDefine(tag, value);
        }

        protected void Insert(string tag, string value)
        {
            _template.Insert(tag, value);
        }

        protected bool Inserted(string tag)
        {
            return _template[$"{tag}+"] != null;
        }

        protected TemplateDocument CloneInner(string tag)
        {
            return _template.CloneInnerTemplate(tag);
        }
    }
}