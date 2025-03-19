using System;
using System.Collections.Generic;

namespace mROA.CodegenTools
{
    public class InsertTemplatePart : ITagged
    {
        private object _insertedValue;
        private string _tag;
        public string Tag => _tag;
        public TemplateDocument Context { get; set; }
        public readonly List<string> Parameters;

        public InsertTemplatePart(string tag, List<string> parameters, TemplateDocument context)
        {
            _tag = tag;
            Parameters = parameters;
            Context = context;
        }

        private bool IsInserted(object value)
        {
            if (Parameters.IndexOf("R") == 0) return false;
            
            ProduceInserted(value);
            
            return true;
        }

        private void ProduceInserted(object value)
        {
            var currentIndex = Context.Parts.IndexOf(this);
            var clone = (InsertTemplatePart)MemberwiseClone();
            clone._tag += "+";

            clone._insertedValue = value;
            
            Context.Parts.Insert(currentIndex, clone);
        }
        
        
        public void Setup(object value)
        {
            if (!(value is string) && !(value is ITemplatePart) )
                return;
            if (IsInserted(value))
                return;

            _insertedValue = value;
        }


        public int TargetLength => 0;

        public string Bake()
        {
            switch (_insertedValue)
            {
                case null:
                    return string.Empty;
                case string s:
                    return s;
                default:
                    return ((ITemplatePart)_insertedValue).Bake();
            }
        }
    }
}