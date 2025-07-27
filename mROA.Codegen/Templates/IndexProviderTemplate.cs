namespace mROA.Codegen.Templates
{
    public class IndexProviderTemplate : TemplateView
    {
        private const string TemplateFile = "IndexProvider.cstmpl";
        
        private const string NamespaceTag = "namespace";
        private const string LevelTag = "level";
        private const string LenTag = "len";
        private const string IndexSpanTag = "indexSpan";
        private const string RemoteTypePairTag = "remoteTypePair";
        
        public IndexProviderTemplate() : base(TemplateFile) { }

        public void DefineNamespace(string value)
        {
            Define(NamespaceTag, value);
        }
        
        public void DefineLevel(string value)
        {
            Define(LevelTag, value);
        }
        
        public void DefineLen(string value)
        {
            Define(LenTag, value);
        }

        public void InsertIndexSpan(string value)
        {
            Insert(IndexSpanTag, value);
        }
        
        public void InsertRemoteTypePair(string value)
        {
            Insert(RemoteTypePairTag, value);
        }

        public bool IsRemoteTypePairInserted()
        {
            return Inserted(RemoteTypePairTag);
        }
    }
}