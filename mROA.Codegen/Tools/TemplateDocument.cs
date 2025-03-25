using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mROA.CodegenTools
{
    public class TemplateDocument : ICloneable
    {
        public List<ITemplateSection> Parts { get; set; } = new List<ITemplateSection>();
        public object AdditionalContext { get; set; }
        public ITagged? this[string tag] => Parts.OfType<ITagged>().FirstOrDefault(i => i.Tag == tag);

        public void Insert(string tag, object value)
        {
            var selectedPart = this[tag];
            if (selectedPart is InsertTemplatePart itp)
            {
                itp.Setup(value);
            }
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

        public void AddDefine(string tag, string text)
        {
            Parts.Add(new DefineTemplateSection(text, tag, this));
        }
    }
}