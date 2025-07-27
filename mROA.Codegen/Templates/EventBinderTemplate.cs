using mROA.CodegenTools;

namespace mROA.Codegen.Templates
{
    public class EventBinderTemplate : TemplateView
    {
        private const string CallFilterTag = "callFilter";
        private const string TypeTag = "type";
        private const string EventNameTag = "eventName";
        private const string ParametersDeclarationTag = "parametersDeclaration";
        private const string CommandIdTag = "commandId";
        private const string TransferParametersTag = "transferParameters";
                
        public EventBinderTemplate(TemplateDocument template) : base(template) { }

        public void DefineCallFilter(string value)
        {
            Define(CallFilterTag, value);
        }
                
        public void DefineType(string value)
        {
            Define(TypeTag, value);
        }
                
        public void DefineEventName(string value)
        {
            Define(EventNameTag, value);
        }
                
        public void DefineParametersDeclaration(string value)
        {
            Define(ParametersDeclarationTag, value);
        }
                
        public void DefineCommandIdTag(string value)
        {
            Define(CommandIdTag, value);
        }
                
        public void DefineTransferParameters(string value)
        {
            Define(TransferParametersTag, value);
        }
    }
}