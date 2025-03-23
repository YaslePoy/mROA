using System;
using System.Collections.Generic;
using System.Linq;

namespace mROA.CodegenTools
{
    public static class TemplateReader
    {
        private static readonly List<Type> ReaderTypes = new List<Type>
        {
            typeof(DefineSectionReader),
            typeof(InsertSectionReader),
            typeof(LinkSectionReader),
            typeof(InnerTemplateSectionReader)
        };

        public static TemplateDocument Parce(string templateText)
        {
            
            int currentIndex = 0;
            var activeReaders = new List<ISectionReader>();

            foreach (var readerType in ReaderTypes)
            {
                var reader = Activator.CreateInstance(readerType) as ISectionReader;
                reader.TemplateText = templateText;
                activeReaders.Add(reader);
            }
            
            var doc = new TemplateDocument();

            while (currentIndex < templateText.Length)
            {
                List<(ISectionReader reader, int index)> indices = activeReaders.Select(i => ((ISectionReader Readers, int index))(i, i.GetNextSectionIndex(currentIndex))).Where(i => i.index > -1).ToList();
                int minIndex = templateText.Length;
                ISectionReader minReader = null;
                if (indices.Count != 0)
                {
                    minIndex = indices.Min(i => i.index);
                    minReader = indices.FirstOrDefault(i => i.index == minIndex).reader;

                    if (indices.Count != activeReaders.Count)
                    {
                        activeReaders = indices.Select(i => i.reader).ToList();
                    }
                }
     
                
                if (currentIndex != minIndex)
                {
                    var literalText = templateText.Substring(currentIndex, minIndex - currentIndex);
                    var literal =
                        new LiteralTemplateSection(literalText, doc);
                    doc.Parts.Add(literal);
                    currentIndex = minIndex;
                }

                if (indices.Count != 0)
                {
                    var section = minReader.ExtractSection(ref currentIndex, doc);
                    doc.Parts.Add(section);
                }
             

            }

            return doc;
        }
    }
}