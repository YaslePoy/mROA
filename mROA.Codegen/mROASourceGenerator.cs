using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace mROA.Codegen
{
    /// <summary>
    /// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
    /// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
    /// </summary>
    [Generator]
    public class mROASourceGenerator : ISourceGenerator
    {
        private void GenerateCode(GeneratorExecutionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
            // For future 
            // var asm = Assembly.GetAssembly(typeof(mROASourceGenerator));
            // var files = asm.GetManifestResourceNames();
            // var test = asm.GetManifestResourceStream("mROA.Codegen.test.tpt");
            // var reader = new StreamReader(test);
            // var allText = reader.ReadToEnd();
            var methods = new List<(string, IMethodSymbol)>();
            var frontendContextRepo = new List<string>();

            var declarations = classes.ToList().OrderBy(i => i.Identifier.Text).ToList();
            foreach (var classDeclarationSyntax in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classDeclarationSyntax.Identifier.Text;
                var innerMembers = CollectMembers(classSymbol);
                var associated = innerMembers.Select(i => i.AssociatedSymbol).Where(i => i != null).Select(i => i!).Distinct(SymbolEqualityComparer.Default).ToList();
                
                var originalName = className;

                className = className.TrimStart('I') + "RemoteEndpoint";

                var remoteEndpointMember = new List<string>();
                
                foreach (var method in innerMembers.OfType<IMethodSymbol>())
                {
                    if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
                        continue;


                    var index = methods.Count;
                    methods.Add((namespaceName + "." + originalName, method));
                    var sb = new StringBuilder();

                    bool isParametrized;

                    bool isAsync;
                    bool isVoid;

                    List<IParameterSymbol>? parameters;

                    switch (method.ReturnType)
                    {
                        case INamedTypeSymbol namedType:
                            isAsync = namedType.Name == "Task";
                            isVoid = isAsync && namedType.TypeParameters.Length == 0 || namedType.Name == "Void";
                            parameters = method.Parameters.ToList();
                            parameters.RemoveAll(i => i.Type.Name is "CancellationToken" or "RequestContext");
                            isParametrized = parameters.Count != 0;
                            break;
                        case IArrayTypeSymbol:
                            isAsync = false;
                            isVoid = false;
                            isParametrized = method.Parameters.Length != 0;
                            break;
                        default:
                            continue;
                    }

                    //Creating signature
                    sb.AppendLine("public" + (isAsync
                                      ? " async "
                                      : " ") +
                                  $"{method.ReturnType.ToDisplayString()} {method.Name}({string.Join(", ", method.Parameters.Select(ToFullString))}){{");


                    var prefix = isAsync ? "await " : "";
                    var postfix = !isAsync ? isVoid ? ".Wait()" : ".GetAwaiter().GetResult()" : "";
                    var parameterLink = isParametrized
                        ? ", new object[] {" + string.Join(", ", method.Parameters.Select(i => i.Name)) + "}"
                        : string.Empty;
                    var tokenInsert = isAsync
                        ? isParametrized
                            ? ", cancellationToken : " + method.Parameters[1].Name
                            : ", cancellationToken : " + method.Parameters[0].Name
                        : String.Empty;
                    var caller = isVoid
                        ? $"CallAsync({index}{parameterLink}{tokenInsert})"
                        : isAsync
                            ? $"GetResultAsync<{ExtractTaskType(method.ReturnType)}>({index}{parameterLink}{tokenInsert})"
                            : $"GetResultAsync<{ToFullString(method.ReturnType)}>({index}{parameterLink}{tokenInsert})";

                    if (!isVoid)
                        prefix = "return " + prefix;

                    sb.AppendLine("\t\t\t" + prefix + caller + postfix + ";");

                    sb.AppendLine("\t\t}");

                    remoteEndpointMember.Add(sb.ToString());
                }

                var code = $@"// <auto-generated/>

using mROA;
using System;
using mROA.Implementation;
using System.Collections.Generic;
using mROA.Abstract;

namespace {namespaceName}
{{
    partial class {className} : RemoteObjectBase, {originalName}
    {{
        public {className}(int id, IRepresentationModule representationModule) : base(id, representationModule)
        {{
        }}

        {string.Join("\r\n\t", remoteEndpointMember)}
    }}
}}
";


                // Add the source code to the compilation.
                // context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));

                frontendContextRepo.Add(
                    $"{{ typeof({classSymbol.ToDisplayString()}), typeof({namespaceName}.{className}) }}");
            }

            if (methods.Count != 0)
            {
                var methodsStringed = Array.Empty<string>();

                var coCodegenRepoCode = @$"// <auto-generated/>
using System.Collections.Generic;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation;
using System;

namespace mROA.Codegen
{{
    public class CoCodegenMethodRepository : IMethodRepository
    {{
        private readonly List<IMethodInvoker> _methods = new () {{
            {string.Join(",\r\n\t\t\t", methodsStringed)}
        }};

        public IMethodInvoker GetMethod(int id)
        {{
            if (id == -1)
                return mROA.Implementation.MethodInvoker.Dispose;

            if (_methods.Count <= id)
                return null;
            
            return _methods[id];
        }}

        public void Inject<T>(T dependency)
        {{
        }}
    }}
}}
";
                context.AddSource("CoCodegenMethodRepository.g.cs", SourceText.From(coCodegenRepoCode, Encoding.UTF8));
            }

            if (frontendContextRepo.Count != 0)
            {
                var fronendRepoCode = @$"// <auto-generated/>
using mROA.Implementation;
using mROA.Abstract;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace mROA.Codegen
{{
    public sealed class RemoteTypeBinder
    {{
        static RemoteTypeBinder(){{
            RemoteContextRepository.RemoteTypes = new Dictionary<Type, Type> {{
            {string.Join(", \r\n\t\t\t", frontendContextRepo)}}};
        }}
    }}
}}
";
                // context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
            }
        }

        private static string ToFullString(IParameterSymbol parameter)
            => /*parameter.Type.ContainingNamespace is null*/
                /*?*/ parameter.ToDisplayString();
        /*: $"{parameter.Type.ContainingNamespace.ToDisplayString()}.{parameter.Type.MetadataName} {parameter.Name}";*/

        private static string ToFullString(ITypeSymbol type) =>
            // => type.ContainingNamespace is null || type.Name == "Void"
            type.ToDisplayString();
        // : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";

        private string ExtractTaskType(ITypeSymbol taskType)
        {
            return (taskType as INamedTypeSymbol).TypeParameters[0].ToDisplayString();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var trees = context.Compilation.SyntaxTrees;

            var interfaces = new List<InterfaceDeclarationSyntax>();
            foreach (var tree in trees)
            {
                var node = tree.GetRoot() as CompilationUnitSyntax;

                foreach (var member in node.Members)
                {
                    if (member is InterfaceDeclarationSyntax ids)
                    {
                        interfaces.Add(ids);
                    }
                    else if (member is NamespaceDeclarationSyntax nds)
                    {
                        foreach (var inside in nds.Members)

                            if (inside is InterfaceDeclarationSyntax ids2)
                                if (ContainsSOIAttribute(ids2.AttributeLists, context, ids2))
                                    interfaces.Add(ids2);
                    }
                }
            }

            GenerateCode(context, context.Compilation, interfaces.ToImmutableArray());
        }

        private bool ContainsSOIAttribute(SyntaxList<AttributeListSyntax> attributes, GeneratorExecutionContext context,
            InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            foreach (var attributeSyntax in
                     attributes.SelectMany(attributeListSyntax => attributeListSyntax.Attributes))
            {
                if (context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree)
                        .GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue; // if we can't get the symbol, ignore it

                string attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the [Report] attribute.
                if (attributeName == "mROA.Implementation.Attributes.SharedObjectInterfaceAttribute")
                    return true;
            }

            return false;
        }

        private List<IMethodSymbol> CollectMembers(INamedTypeSymbol type)
        {
            var methods = type.GetMembers().OfType<IMethodSymbol>().ToList();
            foreach (var inner in type.AllInterfaces)
            {
                methods.AddRange(inner.GetMembers().OfType<IMethodSymbol>());
            }

            methods.RemoveAll(m => m.Name == "Dispose");
            return methods.OrderBy(i => i.Name).ToList();
        }
    }
}