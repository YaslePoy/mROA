namespace mROA.Codegen.Templates
{ 
    public class ProxyTemplate : TemplateView
    {
        private const string TemplateFile = "Proxy.cstmpl";
        
        private const string ClassNameTag = "className";
        private const string OriginalNameTag = "originalName";
        private const string NamespaceNameTag= "namespaceName";
        private const string MethodsTag = "methods";

        public ProxyTemplate() : base(TemplateFile) { }

        public void DefineClassName(string value)
        {
            Define(ClassNameTag, value);
        }
        
        public void DefineOriginalName(string value)
        {
            Define(OriginalNameTag, value);
        }
        
        public void DefineNamespaceName(string value)
        {
            Define(NamespaceNameTag, value);
        }

        public void InsertMethods(string value)
        {
            Insert(MethodsTag, value);
        }
    }
}