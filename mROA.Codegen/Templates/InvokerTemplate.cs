using mROA.CodegenTools;

namespace mROA.Codegen.Templates
{
    public class InvokerTemplate : TemplateView
    {
        private const string IsVoidTag = "isVoid";
        private const string ReturnTypeTag = "returnType";
        private const string ParametersTypeTag = "parametersType";
        private const string SuitableTypeTag = "suitableType";
        private const string FuncInvokingTag = "funcInvoking";
        private const string IsTrustedTag = "isTrusted";

        public InvokerTemplate(TemplateDocument template) : base(template) { }

        public void DefineIsVoid(string value)
        {
            Define(IsVoidTag, value);
        }
            
        public void DefineReturnType(string value)
        {
            Define(ReturnTypeTag, value);
        }
            
        public void DefineParametersType(string value)
        {
            Define(ParametersTypeTag, value);
        }
            
        public void DefineSuitableType(string value)
        {
            Define(SuitableTypeTag, value);
        }
            
        public void DefineFuncInvoking(string value)
        {
            Define(FuncInvokingTag, value);
        }
            
        public void DefineIsTrusted(string value)
        {
            Define(IsTrustedTag, value);
        }
    }
}