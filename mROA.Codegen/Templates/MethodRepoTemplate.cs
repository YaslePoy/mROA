namespace mROA.Codegen.Templates
{
    public class MethodRepoTemplate : TemplateView
    {
        private const string TemplateFile = "MethodRepo.cstmpl";
        
        private const string SyncInvokerTemplate = "syncInvoker";
        private const string AsyncInvokerTemplate = "asyncInvoker";
        
        private const string InvokerTag = "invoker";

        public MethodRepoTemplate() : base(TemplateFile) { }

        public void InsertInvoke(string value)
        {
            Insert(InvokerTag, value);
        }
        
        public InvokerTemplate CloneInnerSyncInvoker()
        {
            var template = CloneInner(SyncInvokerTemplate);
            return new InvokerTemplate(template);
        }
        
        public InvokerTemplate CloneInnerAsyncInvoker()
        {
            var template = CloneInner(AsyncInvokerTemplate);
            return new InvokerTemplate(template);
        }
    }
}