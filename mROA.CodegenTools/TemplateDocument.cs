using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mROA.CodegenTools
{
    public class TemplateDocument
    {
        public List<ITemplatePart> Parts { get; }
        public ITagged this[string tag] => Parts.OfType<ITagged>().FirstOrDefault(i => i.Tag == tag);
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

            for (int i = 0; i < Parts.Count; i++)
            {
                var part = Parts[i];
                var baked = part.Bake();
                stringBuilder.Append(baked);
            }

            return stringBuilder.ToString();
        }
    }
}