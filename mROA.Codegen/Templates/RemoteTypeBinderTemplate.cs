namespace mROA.Codegen.Templates
{
    public class RemoteTypeBinderTemplate : TemplateView
    {
        private const string TemplateFile = "RemoteTypeBinder.cstmpl";
        
        private const string ObjectBinderTemplateTag = "objectBinderTemplate";
        private const string EventBinderTag = "eventBinder";

        public RemoteTypeBinderTemplate() : base(TemplateFile) { }
        
        public ObjectBinderTemplate CloneInnerObjectBinder()
        {
            var template = CloneInner(ObjectBinderTemplateTag);
            return new ObjectBinderTemplate(template);
        }
        
        public void InsertEventBinder(string value)
        {
            Insert(EventBinderTag, value);
        }
    }
}