// See https://aka.ms/new-console-template for more information

using mROA.CodegenTools;

// var doc = new TemplateDocument();
// doc.Parts = new List<ITemplateSection>
// {
//     new LiteralTemplateSection("teset text", doc),
//     new DefineTemplateSection(" another text which is dinided, bun not placed ", "text define", doc),
//     new LinkTemplateSection("text define", doc),
//     new LinkTemplateSection("text define", doc),
//     new LinkTemplateSection("text define", doc),
//     new LiteralTemplateSection(" = = = ", doc),
//     new InsertTemplatePart("insert", doc),
//     new LinkTemplateSection("post", doc)
// };
//
// doc.Insert("insert", "inserted text by insert template => ");
// doc.Parts.Add(new DefineTemplateSection("post added", "post", doc));
// Console.WriteLine(doc.Compile());

var doc = TemplateReader.Parce(File.ReadAllText("exampleTemplate.cstmpl"));
Console.WriteLine(doc.Parts.Count);