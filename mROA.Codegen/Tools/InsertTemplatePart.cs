using System;
using System.Collections.Generic;
using System.Linq;

namespace mROA.CodegenTools
{
    public class InsertTemplatePart : ITagged, IBaking
    {
        private object _insertedValue;
        private string _tag;
        public string Tag => _tag;
        public TemplateDocument Context { get; set; }
        public readonly List<string> Parameters;

        public InsertTemplatePart(string tag, TemplateDocument context, params string[] parameters)
        {
            _tag = tag;
            Parameters = parameters.ToList();
            Context = context;
        }

        private bool IsInserted(object value)
        {
            if (Parameters.IndexOf("r") == -1) return false;
            
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

            InsertSeparator(currentIndex + 1);
        }

        private void InsertSeparator(int currentIndex)
        {
            var sepIndex = Parameters.IndexOf("sep");
            if (sepIndex == -1)
                return;
            
            Context.Parts.Insert(currentIndex, new LiteralTemplateSection(Context[Parameters[sepIndex + 1]].ToString(), Context));
            
        }


        public void Setup(object value)
        {
            if (!(value is string) && !(value is ITemplateSection) )
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
                    return ((ITemplateSection)_insertedValue).ToString();
            }
        }

        public object Clone()
        {
            return new InsertTemplatePart(_tag, Context, Parameters.ToArray());
        }
    }
}