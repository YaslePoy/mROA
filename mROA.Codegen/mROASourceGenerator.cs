using System;
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
        /// <summary>
        /// Checks whether the Node is annotated with the [Report] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
        /// </summary>
        /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
        /// <returns>The specific cast and whether the attribute was found.</returns>
        ///
        ///
        ///
        ///         public void Initialize(IncrementalGeneratorInitializationContext context)
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
        /// 
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
        /// <param name="classDeclarations">Nodes annotated with the [Report] attribute that trigger the generate action.</param>
        private void GenerateCode(GeneratorExecutionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classDeclarations)
        {
            var methods = new List<(string, IMethodSymbol)>();
            var frontendContextRepo = new List<string>();
            // Go through all filtered class declarations.
            foreach (var classDeclarationSyntax in classDeclarations)
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
                    .OfType<IMethodSymbol>();

                var originalName = className;
                // Build up the source code
                className = className.TrimStart('I') + "RemoteEndpoint";


                var methodsText = new List<string>();

                foreach (var method in methodBody)
                {
                    var index = methods.Count;
                    methods.Add((namespaceName + "." + originalName, method));
                    var sb = new StringBuilder();

                    //Creating signature
                    // if (method.Parameters.Length == 0)
                    //     sb.AppendLine($"public {method.ReturnType.ToDisplayString()} {method.Name}(){{");
                    // else

                    bool isAsync = method.ReturnType.Name == "Task";
                    sb.AppendLine("public" + (isAsync
                                      ? " async "
                                      : " ") +
                                  $"{method.ReturnType.ToDisplayString()} {method.Name}({string.Join(", ", method.Parameters.Select(ToFullString))}){{");

                    //Creating request
                    if (method.Parameters.Length == 1 && !isAsync ||
                        method.Parameters.Length == 2 && isAsync)
                    {
                        sb.AppendLine(
                            $"\t\t\tvar defaultCallRequestCodegen = new DefaultCallRequest {{ CommandId = {index}, ObjectId = id, Parameter = {method.Parameters.First().Name} }};");
                    }
                    else
                    {
                        sb.AppendLine(
                            $"\t\t\tvar defaultCallRequestCodegen = new DefaultCallRequest {{ CommandId = {index}, ObjectId = id }};");
                    }

                    //Post created request
                    sb.AppendLine("\t\t\tserialisationModule.PostCallRequest(defaultCallRequestCodegen);");


                    if (method.ReturnType.ToString() == "System.Threading.Tasks.Task")
                    {
                        sb.AppendLine(
                            "\t\t\tawait serialisationModule.GetNextCommandExecution<FinalCommandExecution>(defaultCallRequestCodegen.CallRequestId);");
                    }
                    else if (method.ReturnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>")
                    {
                        var type = method.ReturnType.ToString();
                        type = type.Substring(type.IndexOf('<') + 1);
                        type = type.Substring(0, type.Length - 1);
                        sb.AppendLine(
                            $"\t\t\tvar response = await serialisationModule.GetFinalCommandExecution<{type}>(defaultCallRequestCodegen.CallRequestId);");
                        sb.AppendLine($"\t\treturn ({type})response.Result;");
                    }
                    else if (!isAsync && method.ReturnType.ToDisplayString() != "void")
                    {
                        var type = method.ReturnType.ToDisplayString();
                        sb.AppendLine(
                            $"\t\t\tvar response = serialisationModule.GetFinalCommandExecution<{type}>(defaultCallRequestCodegen.CallRequestId).GetAwaiter().GetResult();");
                        sb.AppendLine($"\t\t\treturn ({type})response.Result;");
                    }

                    sb.AppendLine("\t\t}");

                    methodsText.Add(sb.ToString());
                }

                var code = $@"// <auto-generated/>

using mROA;
using System;
using mROA.Implementation;
using System.Collections.Generic;
using mROA.Abstract;
using System;

namespace {namespaceName} {{

    public class {className} : {originalName}, IRemoteObject
    {{
        private ISerialisationModule.IFrontendSerialisationModule serialisationModule;
        private int id;

        public {className}(int id, ISerialisationModule.IFrontendSerialisationModule serialisationModule){{
            this.id = id;
            this.serialisationModule = serialisationModule;
        }}

        public int Id => id;

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
                        $"typeof({i.Item1}).GetMethod(\"{i.Item2.Name}\", new Type[]{{{string.Join(", ", i.Item2.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))}}})")
                    .ToList();

                var coCodegenRepoCode = @$"// <auto-generated/>
using System.Collections.Generic;
using System.Reflection;
using mROA.Abstract;
using System;

namespace mROA.Codegen {{

    public class CoCodegenMethodRepository : IMethodRepository
    {{
            private readonly List<MethodInfo> _methods = new() {{  
                {string.Join(", \r\n\t\t\t\t", methodsStringed)}
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

using System.Collections.Generic;
using mROA.Implementation;
using mROA.Abstract;
using System;

namespace mROA.Codegen {{

    public class FrontendContextRepository : IContextRepository
    {{
        private ISerialisationModule.IFrontendSerialisationModule _serialisationModule;
        private static readonly Dictionary<Type, Type> _remoteTypes = new Dictionary<Type, Type> {{
            {string.Join(", \r\n\t\t", frontendContextRepo)}}};
        
        public int ResisterObject(object o)
        {{
            throw new NotSupportedException();
        }}

        public void ClearObject(int id)
        {{
            throw new NotSupportedException();
        }}

        public object GetObject(int id)
        {{
            throw new NotSupportedException();
        }}

        public T GetObject<T>(int id)
        {{
            if (_remoteTypes.TryGetValue(typeof(T), out var remoteType))
            {{
                var remote = (T)Activator.CreateInstance(remoteType, id, _serialisationModule)!;
                return remote;
            }}
            throw new NotSupportedException();
        }}

        public object GetSingleObject(Type type)
        {{
            return Activator.CreateInstance(_remoteTypes[type], -1, _serialisationModule)!;
        }}

        public int GetObjectIndex(object o)
        {{
            if (o is IRemoteObject remote)
            {{
                return remote.Id;
            }}
            throw new NotSupportedException();
        }}

        public void Inject<T>(T dependency)
        {{
            if (dependency is ISerialisationModule.IFrontendSerialisationModule serialisationModule)
                _serialisationModule = serialisationModule;
            
            TransmissionConfig.DefaultContextRepository = this;
        }}
    }}
}}
";
                context.AddSource("FrontendContextRepository.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
            }
        }

        private static string ToFullString(IParameterSymbol parameter)
            => $"{parameter.Type.ContainingNamespace.ToDisplayString()}.{parameter.Type.MetadataName} {parameter.Name}";

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
                                interfaces.Add(ids2);
                    }
                }
            }

            GenerateCode(context, context.Compilation, interfaces.ToImmutableArray());
        }
    }
}