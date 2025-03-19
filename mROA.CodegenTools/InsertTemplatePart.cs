using System;
using System.Collections.Generic;

namespace mROA.CodegenTools
{
    public class InsertTemplatePart : ITemplatePart, ITagged
    {
        private string _insertedText = null;
        private ITemplatePart _insertedPart = null;
        private string _tag;
        public string Tag => _tag;
        public TemplateDocument Context { get; set; }
        public List<string> Parameters;

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