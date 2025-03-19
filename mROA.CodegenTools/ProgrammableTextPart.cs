using System;

namespace mROA.CodegenTools
{
    public class ProgrammableTextPart : ITemplatePart
    {
        public TemplateDocument Context { get; set; }
        public int TargetLength => 0;

        private Func<object, string> _bakeFunc;

        public ProgrammableTextPart(Func<object, string> bakeFunc, TemplateDocument context)
        {
            _bakeFunc = bakeFunc;
            Context = context;
        }


        public string Bake()
        {
            return _bakeFunc(Context.AdditionalContext);
        }
    }
}