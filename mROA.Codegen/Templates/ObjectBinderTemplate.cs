using mROA.CodegenTools;

namespace mROA.Codegen.Templates
{
    public partial class ObjectBinderTemplate : TemplateView
    {
        private const string EventBinderTemplateTag = "eventBinderTemplate";
            
        private const string TypeTag = "type";
        private const string EventBinderTag = "eventBinder";

        public ObjectBinderTemplate(TemplateDocument template) : base(template) { }

        public void DefineType(string value)
        {
            Define(TypeTag, value);
        }

        public void InsertEventBinder(string value)
        {
            Insert(EventBinderTag, value);
        }
            
        public EventBinderTemplate CloneInnerEventBinder()
        {
            var template = CloneInner(EventBinderTemplateTag);
            return new EventBinderTemplate(template);
        }
    }
}