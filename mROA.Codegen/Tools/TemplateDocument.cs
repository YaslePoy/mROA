using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mROA.Codegen.Tools
{
    public class TemplateDocument : ICloneable
    {
        public List<ITemplateSection> Parts { get; set; } = new();
        public object AdditionalContext { get; set; }
        public ITagged? this[string tag] => Parts.OfType<ITagged>().FirstOrDefault(i => i.Tag == tag);

        public object Clone()
        {
            var cloneDoc = new TemplateDocument
            {
                AdditionalContext = AdditionalContext is ICloneable c ? c.Clone() : AdditionalContext
            };

            cloneDoc.Parts = Parts.Select(i =>
            {
                var clone = i.Clone() as ITemplateSection;
                clone.Context = cloneDoc;
                return clone;
            }).ToList();

            return cloneDoc;
        }

        public void Insert(string tag, object value)
        {
            var selectedPart = this[tag];
            if (selectedPart is InsertTemplateSection itp) itp.Setup(value);
        }

        public string Compile()
        {
            var approximatelyLength = Parts.Sum(i => i.TargetLength);
            var stringBuilder = new StringBuilder(approximatelyLength);

            foreach (var part in Parts.OfType<IBaking>())
            {
                var baked = part.Bake();
                stringBuilder.Append(baked);
            }

            return stringBuilder.ToString();
        }

        public void AddDefine(string tag, string text)
        {
            Parts.Add(new DefineTemplateSection(text, tag, this));
        }

        public void AddDefine(string tag, object value)
        {
            AddDefine(tag, value.ToString());
        }
    }
}