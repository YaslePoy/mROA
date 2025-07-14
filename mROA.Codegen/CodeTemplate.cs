using System;
using mROA.CodegenTools;
using mROA.CodegenTools.Reading;

namespace mROA.Codegen
{
    public class CodeTemplate
    {
        private const string MethodRepoTemplateName = "MethodRepo.cstmpl";
        private const string ProxyTemplateName = "Proxy.cstmpl";
        private const string RemoteTypeBinderTemplateName = "RemoteTypeBinder.cstmpl";
        private const string PartialInterfaceTemplateName = "PartialInterface.cstmpl";
        private const string IndexProviderTemplateName = "IndexProvider.cstmpl";
        private const string SyncInvokerTag = "syncInvoker";

        private TemplateDocument? _proxy;
        private TemplateDocument? _indexerProvider;
        private TemplateDocument? _remoteTypeBinder;
        private TemplateDocument? _partialInterface;
        private TemplateDocument? _methodRepo;
        private TemplateDocument? _methodInvoker;

        public TemplateDocument Proxy => _proxy 
                                         ?? throw new InvalidOperationException();
        public TemplateDocument IndexerProvider => _indexerProvider 
                                                   ?? throw new InvalidOperationException();
        public TemplateDocument RemoteTypeBinder => _remoteTypeBinder 
                                                    ?? throw new InvalidOperationException();
        public TemplateDocument PartialInterface => _partialInterface 
                                                    ?? throw new InvalidOperationException();
        public TemplateDocument MethodRepo => _methodRepo 
                                              ?? throw new InvalidOperationException();
        public TemplateDocument MethodInvoker => _methodInvoker 
                                                 ?? throw new InvalidOperationException();

        public void LoadTemplates()
        {
            _proxy = TemplateReader.FromEmbeddedResource(ProxyTemplateName);
            _indexerProvider = TemplateReader.FromEmbeddedResource(IndexProviderTemplateName);
            _remoteTypeBinder = TemplateReader.FromEmbeddedResource(RemoteTypeBinderTemplateName);
            _partialInterface = TemplateReader.FromEmbeddedResource(PartialInterfaceTemplateName);
            _methodRepo = TemplateReader.FromEmbeddedResource(MethodRepoTemplateName);

            var innerMethodSection = MethodRepo[SyncInvokerTag] as InnerTemplateSection 
                                     ?? throw new InvalidOperationException();
            _methodInvoker = innerMethodSection.InnerTemplate;
        }
    }
}