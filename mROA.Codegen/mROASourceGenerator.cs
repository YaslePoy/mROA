// #define DONT_ADD

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
        private static Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        private static Predicate<ITypeSymbol> ParameterFilterForType =
            i => i.Name is "CancellationToken" or "RequestContext";

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
            var eventBinders = new List<string>();
            List<IMethodSymbol> totalMethods = new List<IMethodSymbol>();


            List<string> invokers = new List<string>();
            var declarations = classes.ToList().OrderBy(i => i.Identifier.Text).ToList();
            foreach (var classDeclarationSyntax in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;
                List<IMethodSymbol> innerMethods = new List<IMethodSymbol>();

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classDeclarationSyntax.Identifier.Text;
                innerMethods = CollectMembers(classSymbol);
                var associated = innerMethods.Select(i => i.AssociatedSymbol).Where(i => i != null)
                    .Distinct(SymbolEqualityComparer.Default).Cast<ISymbol>().ToList();

                innerMethods = innerMethods.Where(i =>
                        i.MethodKind != MethodKind.EventAdd && i.MethodKind != MethodKind.EventRemove)
                    .ToList();

                totalMethods.AddRange(innerMethods);

                var originalName = className;

                className = className.TrimStart('I') + "RemoteEndpoint";


                var declaredMethods = new List<string>();
                var propertiesAccessMethods = new List<(string, IMethodSymbol)>();

                foreach (var method in innerMethods)
                {
                    switch (method.MethodKind)
                    {
                        case MethodKind.PropertyGet or MethodKind.PropertySet:
                            GeneratePropertyMethod(method, propertiesAccessMethods, invokers,
                                classSymbol);
                            continue;
                        default:
                            GenerateDeclaredMethod(method, declaredMethods, invokers, classSymbol);
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

                GenerateEventImplementation(classSymbol, invokers, declaredMethods, context, eventBinders);

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

            if (totalMethods.Count != 0)
            {
                var methodsStringed = invokers;

                var coCodegenRepoCode = @$"// <auto-generated/>
using System.Collections.Generic;
using System.Reflection;
using mROA.Abstract;
using mROA.Implementation;
using System;
using System.Threading;

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
#if !DONT_ADD
                context.AddSource("CoCodegenMethodRepository.g.cs", SourceText.From(coCodegenRepoCode, Encoding.UTF8));
#endif
            }

            if (frontendContextRepo.Count != 0)
            {
                var fronendRepoCode = @$"// <auto-generated/>
using System.Collections.Generic;
using System.Reflection;
using System;
using mROA.Abstract;
using mROA.Implementation.Backend;
using mROA.Implementation;

namespace mROA.Codegen
{{
    public sealed class RemoteTypeBinder
    {{
        static RemoteTypeBinder(){{
            RemoteContextRepository.RemoteTypes = new Dictionary<Type, Type> {{
            {string.Join(", \r\n\t\t\t", frontendContextRepo)}}};
            ContextRepository.EventBinders = new object[] {{
            {string.Join(",\r\n\t\t\t", eventBinders)}}};
        }}
    }}
}}
";
#if !DONT_ADD
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
#endif
            }
        }

        private void GenerateEventImplementation(INamedTypeSymbol classSymbol, List<string> invokers,
            List<string> declaredMethods, GeneratorExecutionContext context, List<string> binders)
        {
            var events = classSymbol.AllInterfaces.Add(classSymbol).SelectMany(i => i.GetMembers())
                .OfType<IEventSymbol>().ToList();
            if (events.Count == 0)
                return;

            var additionalSignatures = new List<string>(events.Count);
            var singleEventBinder = new List<string>(events.Count);
            for (int i = 0; i < events.Count; i++)
            {
                var currentEvent = events[i];

                var additionalMethod = GenerateMethodExternalCaller(currentEvent, out var signature);
                declaredMethods.Add(additionalMethod);
                additionalSignatures.Add(signature);
                GenerateEventCode(currentEvent, invokers, classSymbol);
                GenerateBinderCode(currentEvent, invokers, classSymbol, singleEventBinder);
            }

            var partialInterface = $@"
namespace {classSymbol.ContainingNamespace.ToDisplayString()}
{{
    public partial interface {classSymbol.Name}
    {{
{string.Join("\r\n", additionalSignatures)}
    }}
}}
";
            var binder = $@"new EventBinder<{classSymbol.ToDisplayString()}>
        {{
            BindAction = (instance, context, representationProducer, index) =>
            {{
                var module = representationProducer.Produce(context.OwnerId);

{string.Join("\r\n", singleEventBinder)}
            }}
}}";
            binders.Add(binder);
#if !DONT_ADD
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(partialInterface, Encoding.UTF8));
#endif
        }

        private string GenerateMethodExternalCaller(IEventSymbol eventSymbol, out string interfaceSignature)
        {
            var level = "\t\t";
            var parameters = (eventSymbol.Type as INamedTypeSymbol).TypeArguments;
            var parameterIndex = 0;
            var parametersDeclaration =
                string.Join(", ", parameters.Select(i => $"{i.ToDisplayString()} p{parameterIndex++}"));
            var signature = $@"public void {EventExternalName(eventSymbol)}({parametersDeclaration})";
            interfaceSignature = level + signature + ";";
            var caller = $@"{signature}
{level}{{
{level}    {eventSymbol.Name}?.Invoke({string.Join(", ", Enumerable.Range(0, parameterIndex).Select(i => "p" + i))});
{level}}} 
";
            return caller;
        }

        public static string EventExternalName(IEventSymbol eventSymbol) => $"{eventSymbol.Name}External";

        private static string Caster(ITypeSymbol type, string inner)
        {
            if (!type.IsValueType)
                return inner +
                       " as " +
                       type.ToDisplayString();
            return $"({type.ToDisplayString()})" + inner;
        }

        private void GenerateDeclaredMethod(IMethodSymbol method, List<string> declaredMethods, List<string> invokers,
            INamedTypeSymbol baseInterace)
        {
            var index = invokers.Count;
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

            declaredMethods.Add(sb.ToString());
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))}");
            var level = "\t\t\t";

            var parametersInsertList = new List<string>();

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var parameter = method.Parameters[i];
                switch (parameter.Type.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("special[0] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(Caster(parameter.Type,
                            $"parameters[{parameters.IndexOf(parameter)}]"));
                        break;
                }
            }

            var parametersInsert = string.Join(", ", parametersInsertList);
            var backend = string.Empty;

            var funcInvoking = string.Empty;

            if (isAsync && !isVoid)
                funcInvoking =
                    $"(i as {method.ContainingType.ToDisplayString()}).{method.Name}({parametersInsert}).ContinueWith(t => {{ post(t.Result); }})";
            else if (isAsync && isVoid)
                funcInvoking =
                    $"(i as {method.ContainingType.ToDisplayString()}).{method.Name}({parametersInsert}).ContinueWith(t => {{ post(null); }})";
            else if (!isAsync && isVoid)
            {
                funcInvoking = $@"{{
{level}        (i as {method.ContainingType.ToDisplayString()}).{method.Name}({parametersInsert});
{level}        return null;
{level}    }}";
            }
            else
            {
                funcInvoking = $"(i as {method.ContainingType.ToDisplayString()}).{method.Name}({parametersInsert})";
            }

            if (isAsync)
                backend = $@"new mROA.Implementation.AsyncMethodInvoker 
{level}{{
{level}    IsVoid = {isVoid.ToString().ToLower()},
{(isVoid ? String.Empty : (level + "\t" + "ReturnType = typeof(" + ExtractTaskType(method.ReturnType)) + "),")}
{level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, parameters, special, post) => {funcInvoking},
{level}}}";
            else
                backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = {isVoid.ToString().ToLower()},
{level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
{level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, parameters, special) => {funcInvoking}
{level}}}";

            invokers.Add(backend);
        }

        private void GenerateBinderCode(IEventSymbol eventSymbol, List<string> invokers, INamedTypeSymbol baseType,
            List<string> binders)
        {
            var index = invokers.Count - 1;
            var parameters = (eventSymbol.Type as INamedTypeSymbol).TypeArguments.ToList();
            int parameterIndex = 0;
            var parametersDeclaration = string.Join(", ",
                JoinWithComa(Enumerable.Range(0, parameters.Count).Select(i => "p" + i++)));

            var transferParameters =
                JoinWithComa(parameters.Where(i => !ParameterFilterForType(i))
                    .Select(i => "p" + parameters.IndexOf(i)));

            var callFilter = "";

            var requestIndex = parameters.FindIndex(i => i.Name == "RequestContext");
            if (requestIndex != -1)
            {
                callFilter = $"\n\r\t\t\tif(context.OwnerId == p{requestIndex}.OwnerId) return;";
            }

            var eventBinderCode =
                $@"                (instance as {baseType.ToDisplayString()}).{eventSymbol.Name} += ({parametersDeclaration}) => 
                    {{
                        Console.WriteLine(""Sending event..."");
{callFilter}
                        var request = new DefaultCallRequest
                        {{ 
                            CommandId = {index}, ObjectId = new UniversalObjectIdentifier(index, context.OwnerId), Parameters = new object[] {{ {transferParameters} }}
                        }};
                        module.PostCallMessageAsync(request.Id, MessageType.EventRequest, request);
                    }};
";
            binders.Add(eventBinderCode);
        }

        public static string JoinWithComa(IEnumerable<string> parts) => string.Join(", ", parts);

        private void GenerateEventCode(IEventSymbol eventSymbol, List<string> invokers, ITypeSymbol baseInterface)
        {
            var level = "\t\t\t";

            var parameters = (eventSymbol.Type as INamedTypeSymbol).TypeArguments;
            var parsingParameters = parameters.RemoveAll(ParameterFilterForType).ToList();
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parsingParameters.Select(p => $"typeof({p.ToDisplayString()})"))}");

            var parametersInsertList = new List<string>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                switch (parameter.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("special[1] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(Caster(parameter,
                            $"parameters[{parameters.IndexOf(parameter)}]"));
                        break;
                }
            }

            var parametersInsert = string.Join(", ", parametersInsertList);
            var backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = true,
{level}    ReturnType = typeof(void),
{level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
{level}    SuitableType = typeof({baseInterface.ToDisplayString()}),
{level}    Invoking = (i, parameters, special) => {{
{level}        (i as {baseInterface.ToDisplayString()}).{EventExternalName(eventSymbol)}({parametersInsert});
{level}        return null;
{level}    }}
{level}}}";
            invokers.Add(backend);
        }

        private void GeneratePropertyMethod(IMethodSymbol method,
            List<(string, IMethodSymbol)> propsCollection, List<string> invokers, INamedTypeSymbol baseInterace)
        {
            var level = "\t\t\t";
            var index = invokers.Count;
            string frontend;
            string backend = string.Empty;
            if (method.MethodKind == MethodKind.PropertyGet)
            {
                var parametersArray = "";
                if (method.Parameters.Length != 0)
                {
                    parametersArray = $", new object[] {{{string.Join(", ", method.Parameters.Select(p => p.Name))}}}";

                    var parameterTypes = string.Join(", ",
                        $"{string.Join(", ", method.Parameters.Select(p => "typeof(" + p.Type.ToDisplayString() + ")"))}");
                    var parameterInserts = string.Join(", ",
                        method.Parameters.Select(
                            p => Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));
                    backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = false,
{level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
{level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()})[{parameterInserts}],
{level}}}";
                }
                else
                {
                    backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = false,
{level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, _, _) => (i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name},
{level}}}";
                }

                frontend =
                    $"get => GetResultAsync<{method.ReturnType.ToDisplayString()}>({index}{parametersArray}).GetAwaiter().GetResult();";
            }
            else
            {
                var parametersArray = "value";
                if (method.Parameters.Length != 1)
                {
                    parametersArray = string.Join(", ", method.Parameters.Select(p => p.Name));

                    var parameterTypes = string.Join(", ",
                        $"{string.Join(", ", method.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))}");
                    var parameterInserts = string.Join(", ",
                        method.Parameters.Take(method.Parameters.Length - 1).Select(p =>
                            {
                                return Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]");
                                // if (!p.Type.IsValueType)
                                //     return "parameters[" + method.Parameters.IndexOf(p) + "] as " +
                                //            p.Type.ToDisplayString();
                                // return $"({p.Type.ToDisplayString()})parameters[{method.Parameters.IndexOf(p)}]";
                            }
                        ));

                    // var valueInsert = !method.Parameters.Last().Type.IsValueType
                    //     ? "parameters[" + (method.Parameters.Length - 1) + "] as " +
                    //       method.Parameters.Last().Type.ToDisplayString()
                    //     : $"({method.Parameters.Last().Type.ToDisplayString()})parameters[{method.Parameters.Length - 1}]";
                    var valueInsert = Caster(method.Parameters.Last().Type,
                        "parameters[" + (method.Parameters.Length - 1) + "]");
                    backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = true,
{level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
{level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()})[{parameterInserts}] = {valueInsert},
{level}}}";
                }
                else
                {
                    backend = $@"new mROA.Implementation.MethodInvoker 
{level}{{
{level}    IsVoid = true,
{level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
{level}    ParameterTypes = new Type[] {{ typeof({method.Parameters.First().Type.ToDisplayString()}) }},
{level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
{level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")},
{level}}}";
                }

                frontend = $"set => CallAsync({index}, new object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((frontend, method));
            invokers.Add(backend);
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
            return (taskType as INamedTypeSymbol).TypeArguments[0].ToDisplayString();
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