using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private const string Namespace = "mROA.Implementation";
        private const string AttributeName = "SharedObjectInterafceAttribute";

        private const string AttributeSourceCode = $@"// <auto-generated/>

namespace {Namespace}
{{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class {AttributeName} : System.Attribute
    {{
    }}
}}";

        // public void Initialize(IncrementalGeneratorInitializationContext context)
        // {
        //     // Filter classes annotated with the [Report] attribute. Only filtered Syntax Nodes can trigger code generation.
        //     var provider = context.SyntaxProvider
        //         .CreateSyntaxProvider(
        //             (s, _) => s is InterfaceDeclarationSyntax,
        //             (ctx, _) => GetClassDeclarationForSourceGen(ctx))
        //         .Where(t => t.reportAttributeFound)
        //         .Select((t, _) => t.Item1);
        //
        //     // Generate the source code.
        //     context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
        //         ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
        // }

        /// <summary>
        /// Checks whether the Node is annotated with the [Report] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
        /// </summary>
        /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
        /// <returns>The specific cast and whether the attribute was found.</returns>
        // private static (InterfaceDeclarationSyntax, bool reportAttributeFound) GetClassDeclarationForSourceGen(
        //     GeneratorSyntaxContext context)
        // {
        //     var classDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;
        //
        //     // Go through all attributes of the class.
        //     foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        //     foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
        //     {
        //         if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
        //             continue; // if we can't get the symbol, ignore it
        //
        //         string attributeName = attributeSymbol.ContainingType.ToDisplayString();
        //
        //         // Check the full name of the [Report] attribute.
        //         if (attributeName == "mROA.Implementation.Attributes.SharedObjectInterfaceAttribute")
        //             return (classDeclarationSyntax, true);
        //     }
        //
        //     return (classDeclarationSyntax, false);
        // }

        /// <summary>
        /// Generate code action.
        /// It will be executed on specific nodes (ClassDeclarationSyntax annotated with the [Report] attribute) changed by the user.
        /// </summary>
        /// <param name="context">Source generation context used to add source files.</param>
        /// <param name="compilation">Compilation used to provide access to the Semantic Model.</param>
        /// <param name="classes">Nodes annotated with the [Report] attribute that trigger the generate action.</param>
        private void GenerateCode(GeneratorExecutionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
            var methods = new List<(string, IMethodSymbol)>();
            var frontendContextRepo = new List<string>();
            // Go through all filtered class declarations.
            var declarations = classes.ToList().OrderBy(i => i.Identifier.Text).ToList();
            foreach (var classDeclarationSyntax in declarations)
            {
                // We need to get semantic model of the class to retrieve metadata.
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);


                // Symbols allow us to get the compile-time information.
                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;


                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                // 'Identifier' means the token of the node. Get class name from the syntax node.
                var className = classDeclarationSyntax.Identifier.Text;

                // Go through all class members with a particular type (property) to generate method lines.
                var methodBody = classSymbol.GetMembers()
                    .OfType<IMethodSymbol>().OrderBy(i => i.Name);

                var originalName = className;
                // Build up the source code
                className = className.TrimStart('I') + "RemoteEndpoint";


                var methodsText = new List<string>();

                foreach (var method in methodBody)
                {
                    var index = methods.Count;
                    methods.Add((namespaceName + "." + originalName, method));
                    var sb = new StringBuilder();


                    bool isAsync = method.ReturnType.Name == "Task";
                    bool isVoid = method.ReturnType.Name == "Void" || method.ReturnType.ToString() == "Task";
                    bool isParametrized = method.Parameters.Length == 1 && !isAsync ||
                                          method.Parameters.Length == 2 && isAsync;


                    //Creating signature
                    sb.AppendLine("public" + (isAsync
                                      ? " async "
                                      : " ") +
                                  $"{method.ReturnType.ToDisplayString()} {method.Name}({string.Join(", ", method.Parameters.Select(ToFullString))}){{");


                    var prefix = isAsync ? "await " : "";
                    var postfix = !isAsync ? (isVoid ? ".Wait()" : ".GetAwaiter().GetResult()") : "";
                    var parameterLink = isParametrized ? ", " + method.Parameters.First().Name : string.Empty;
                    var caller = isVoid ? $"CallAsync({index}{parameterLink})" :
                        isAsync ? $"GetResultAsync<{ExtractTaskType(method.ReturnType)}>({index}{parameterLink})" :
                        $"GetResultAsync<{ToFullString(method.ReturnType)}>({index}{parameterLink})";

                    if (!isVoid)
                        prefix = "return " + prefix;

                    // if (method.ReturnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>")
                    // {
                    //     var type = method.ReturnType.ToString();
                    //     type = type.Substring(type.IndexOf('<') + 1);
                    //     type = type.Substring(0, type.Length - 1);
                    //     sb.AppendLine(
                    //         $"\t\tvar response = await serialisationModule.GetFinalCommandExecution<{type}>(defaultCallRequestCodegen.CallRequestId);");
                    //     sb.AppendLine($"\t\treturn ({type})response.Result;");
                    // }
                    // else if (!isAsync && method.ReturnType.ToDisplayString() != "void")
                    // {
                    //     var type = method.ReturnType.ToDisplayString();
                    //     sb.AppendLine(
                    //         $"\t\tvar response = serialisationModule.GetFinalCommandExecution<{type}>(defaultCallRequestCodegen.CallRequestId).GetAwaiter().GetResult();");
                    //     sb.AppendLine($"\t\treturn ({type})response.Result;");
                    // }
                    // else
                    // {
                    //     sb.AppendLine(
                    //         "\t\tserialisationModule.GetNextCommandExecution<FinalCommandExecution>(defaultCallRequestCodegen.CallRequestId).Wait();");
                    // }
                    //
                    // sb.AppendLine("\t}");

                    sb.AppendLine("\t\t\t" + prefix + caller + postfix + ";");

                    sb.AppendLine("\t\t}");

                    methodsText.Add(sb.ToString());
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

        {string.Join("\r\n\t", methodsText)}
    }}
}}
";



                // Add the source code to the compilation.
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));

                frontendContextRepo.Add(
                    $"{{ typeof({classSymbol.ToDisplayString()}), typeof({namespaceName}.{className}) }}");
            }

            if (methods.Count != 0)
            {
                var methodsStringed = methods.Select(i =>
                        $"typeof({i.Item1}).GetMethod(\"{i.Item2.Name}\", new Type[] {{{string.Join(", ", i.Item2.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))}}})")
                    .ToList();

                var coCodegenRepoCode = @$"// <auto-generated/>
using System.Collections.Generic;
using System.Reflection;
using mROA.Abstract;
using System;

namespace mROA.Codegen
{{
    public class CoCodegenMethodRepository : IMethodRepository
    {{
        private readonly List<MethodInfo> _methods = new () {{
            {string.Join(",\r\n\t\t\t", methodsStringed)}
        }};

        public MethodInfo GetMethod(int id)
        {{
            if (_methods.Count <= id)
                return null;
            
            return _methods[id];
        }}

        public int RegisterMethod(MethodInfo method)
        {{
            _methods.Add(method);
            return _methods.Count - 1;
        }}

        public IEnumerable<MethodInfo> GetMethods()
        {{
            return _methods;
        }}

        public void Inject<T>(T dependency)
        {{
        }}
    }}
}}
";
                context.AddSource($"CoCodegenMethodRepository.g.cs", SourceText.From(coCodegenRepoCode, Encoding.UTF8));
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
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
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
            var type = taskType.ToString();
            type = type.Substring(type.IndexOf('<') + 1);
            return type.Substring(0, type.Length - 1);
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
            foreach (AttributeListSyntax attributeListSyntax in attributes)
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
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
    }
}