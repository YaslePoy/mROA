namespace mROA.Codegen.Templates
{
    public class PartialInterfaceTemplate : TemplateView
    {
        private const string TemplateFile = "PartialInterface.cstmpl";
        
        private const string NameTag = "name";
        private const string NamespaceTag = "namespace";
        private const string SignatureTag = "signature";

        public PartialInterfaceTemplate() : base(TemplateFile) { }

        public void DefineName(string value)
        {
            Define(NameTag, value);
        }
        
        public void DefineNamespace(string value)
        {
            Define(NamespaceTag, value);
        }

        public void InsertSignature(string value)
        {
            Insert(SignatureTag, value);
        }
    }
}