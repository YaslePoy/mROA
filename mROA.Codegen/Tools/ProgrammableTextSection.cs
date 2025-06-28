using System;

namespace mROA.CodegenTools
{
    public class ProgrammableTextSection : ITemplateSection, IBaking
    {
        private readonly Func<object, string> _bakeFunc;

        public ProgrammableTextSection(Func<object, string> bakeFunc, TemplateDocument context)
        {
            _bakeFunc = bakeFunc;
            Context = context;
        }


        public string Bake()
        {
            return _bakeFunc(Context.AdditionalContext);
        }

        public TemplateDocument Context { get; set; }
        public int TargetLength => 0;

        public object Clone()
        {
            return new ProgrammableTextSection(_bakeFunc, Context);
        }
    }
}