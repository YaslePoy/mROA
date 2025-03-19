using System;
using System.Collections.Generic;

namespace mROA.CodegenTools
{
    public class InsertTemplatePart : ITemplatePart, ITagged
    {
        private string _insertedText;
        private ITemplatePart _insertedPart;
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
            if (!(value is string) && !(value is ITemplatePart) )
                return false;

            if (Parameters.IndexOf("R") == 0) return false;
            
            ProduceInserted(value);
            
            return true;
        }

        private void ProduceInserted(object value)
        {
            var currentIndex = Context.Parts.IndexOf(this);
            var clone = (InsertTemplatePart)MemberwiseClone();
            clone._tag += "+";
            
            if (value is string s)
            {
                clone.Setup(s);
            }else
                clone.Setup((ITemplatePart)value);
            
            Context.Parts.Insert(currentIndex, clone);
        }
        
        
        public void Setup(string inside)
        {
            if (IsInserted(inside))
                return;
            _insertedText = inside;
        }

        public void Setup(ITemplatePart part)
        {
            if (IsInserted(part))
                return;
            _insertedPart = part;
        }
        
        public string Bake()
        {
            if (_insertedText != null)
            {
                return _insertedText;
            }

            return _insertedPart.Bake();
        }
    }
}