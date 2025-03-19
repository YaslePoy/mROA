using System.Linq;

namespace mROA.CodegenTools
{
    public class LinkTemplatePart : ITemplatePart
    {
        private readonly string _linkedTag;
        public TemplateDocument Context { get; set; }

        public string LinkedTag => _linkedTag;

        public string Bake()
        {
            return Context.Parts.OfType<ITagged>().FirstOrDefault(i => i.Tag == _linkedTag)?.Bake();
        }
    }
}