using System.Collections.Generic;
using System.Linq;

namespace mROA.Codegen.Tools
{
    public class InsertTemplateSection : ITagged, IBaking
    {
        public readonly List<string> Parameters;
        private object _insertedValue;

        public InsertTemplateSection(string tag, TemplateDocument context, params string[] parameters)
        {
            Tag = tag;
            Parameters = parameters.ToList();
            Context = context;
        }

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

        public string Tag { get; private set; }

        public TemplateDocument Context { get; set; }


        public int TargetLength => 0;

        public object Clone()
        {
            return new InsertTemplateSection(Tag, Context, Parameters.ToArray());
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
            var clone = (InsertTemplateSection)MemberwiseClone();
            clone.Tag += "+";

            clone._insertedValue = value;

            Context.Parts.Insert(currentIndex, clone);

            InsertSeparator(currentIndex + 1);
        }

        private void InsertSeparator(int currentIndex)
        {
            var sepIndex = Parameters.IndexOf("sep");
            if (sepIndex == -1)
                return;

            Context.Parts.Insert(currentIndex,
                new LiteralTemplateSection(Context[Parameters[sepIndex + 1]].ToString(), Context));
        }


        public void Setup(object value)
        {
            if (!(value is string) && !(value is ITemplateSection))
                return;
            if (IsInserted(value))
                return;

            _insertedValue = value;
        }
    }
}