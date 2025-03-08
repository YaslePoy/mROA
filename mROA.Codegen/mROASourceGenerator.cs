// #define DONT_ADD

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
            var frontendContextRepo = new List<string>();

            List<IMethodSymbol> innerMethods = new List<IMethodSymbol>();
            var declarations = classes.ToList().OrderBy(i => i.Identifier.Text).ToList();
            foreach (var classDeclarationSyntax in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classDeclarationSyntax.Identifier.Text;
                innerMethods = CollectMembers(classSymbol);
                var associated = innerMethods.Select(i => i.AssociatedSymbol).Where(i => i != null)
                    .Distinct(SymbolEqualityComparer.Default).Cast<ISymbol>().ToList();

                innerMethods = innerMethods.Where(i =>
                        i.MethodKind != MethodKind.EventAdd && i.MethodKind != MethodKind.EventRemove)
                    .ToList();

                var originalName = className;

                className = className.TrimStart('I') + "RemoteEndpoint";


                var declaredMethods = new List<string>();
                var propertiesAccessMethods = new List<(string, IMethodSymbol)>();
                var propertiesImplementations = new List<string>(associated.Count);
                foreach (var method in innerMethods)
                {
                    switch (method.MethodKind)
                    {
                        case MethodKind.PropertyGet or MethodKind.PropertySet:
                            GeneratePropertyMethod(method, innerMethods, propertiesAccessMethods);
                            continue;
                        default:
                            GenerateDeclaretedMethod(method, declaredMethods, innerMethods);
                            break;
                    }
                }

                foreach (var symbol in associated)
                {
                    switch (symbol)
                    {
                        case IPropertySymbol propertySymbol:

                            var setter = propertiesAccessMethods.FirstOrDefault(i =>
                                i.Item2.AssociatedSymbol!.Name == propertySymbol.Name && i.Item2.ReturnsVoid);
                            var getter = propertiesAccessMethods.FirstOrDefault(i =>
                                i.Item2.AssociatedSymbol!.Name == propertySymbol.Name && !i.Item2.ReturnsVoid);
                            string impl;
                            if (propertySymbol.IsIndexer)
                            {
                                impl =
                                    $"public {propertySymbol.Type.ToDisplayString()} this[{string.Join(", ", propertySymbol.Parameters.Select(p => p.ToDisplayString()))}] {{ {getter.Item1} {setter.Item1} }}";
                            }
                            else
                            {
                                impl =
                                    $"public {propertySymbol.Type.ToDisplayString()} {symbol.Name} {{ {getter.Item1} {setter.Item1} }}";
                            }

                            declaredMethods.Add(impl);
                            break;
                        case IEventSymbol eventSymbol:
                            declaredMethods.Add(
                                $"public event {eventSymbol.Type.ToDisplayString()}? {eventSymbol.Name};");
                            break;
                    }
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

        {string.Join("\r\n\t\t", declaredMethods)}
    }}
}}
";


                // Add the source code to the compilation.
#if !DONT_ADD
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                frontendContextRepo.Add(
                    $"{{ typeof({classSymbol.ToDisplayString()}), typeof({namespaceName}.{className}) }}");
            }

            if (innerMethods.Count != 0)
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
#if !DONT_ADD
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
#endif
            }
        }

        private void GenerateDeclaretedMethod(IMethodSymbol method, List<string> declaretedMethods,
            List<IMethodSymbol> methods)
        {
            var index = methods.IndexOf(method);
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
                    parameters.RemoveAll(ParameterFilter);
                    isParametrized = parameters.Count != 0;
                    break;
                case IArrayTypeSymbol:
                    isAsync = false;
                    isVoid = false;
                    parameters = new List<IParameterSymbol>();
                    isParametrized = method.Parameters.Length != 0;
                    break;
                default:
                    return;
            }

            //Creating signature
            sb.AppendLine("public" + (isAsync
                              ? " async "
                              : " ") +
                          $"{method.ReturnType.ToDisplayString()} {method.Name}({string.Join(", ", method.Parameters.Select(ToFullString))}){{");


            var prefix = isAsync ? "await " : "";
            var postfix = !isAsync ? isVoid ? ".Wait()" : ".GetAwaiter().GetResult()" : "";
            var parameterLink = isParametrized
                ? ", new object[] {" + string.Join(", ", parameters.Select(i => i.Name)) + "}"
                : string.Empty;
            var tokenInsert = isAsync && method.Parameters.FirstOrDefault(i => i.Type.Name == "CancellationToken") is
                { } tokenSymbol
                ? ", cancellationToken : " + tokenSymbol.Name
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

            declaretedMethods.Add(sb.ToString());
        }

        private static Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        public void GeneratePropertyMethod(IMethodSymbol method, List<IMethodSymbol> methods,
            List<(string, IMethodSymbol)> propsCollection)
        {
            var index = methods.IndexOf(method);
            var sb = "";
            if (method.MethodKind == MethodKind.PropertyGet)
            {
                var parametersArray = "";
                if (method.Parameters.Length != 0)
                {
                    parametersArray = $", new object[] {{{string.Join(", ", method.Parameters.Select(p => p.Name))}}}";
                }

                sb =
                    $"get => GetResultAsync<{method.ReturnType.ToDisplayString()}>({index}{parametersArray}).GetAwaiter().GetResult();";
            }
            else
            {
                var parametersArray = "value";
                if (method.Parameters.Length != 0)
                {
                    parametersArray = string.Join(", ", method.Parameters.Select(p => p.Name));
                }

                sb = $"set => CallAsync({index}, new object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((sb.ToString(), method));
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