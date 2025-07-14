// #define DONT_ADD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using mROA.CodegenTools;
using mROA.CodegenTools.Reading;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace mROA.Codegen
{
    /// <summary>
    ///     A sample source generator that creates a custom report based on class properties. The target class should be
    ///     annotated with the 'Generators.ReportAttribute' attribute.
    ///     When using the source code as a baseline, an incremental source generator is preferable because it reduces the
    ///     performance overhead.
    /// </summary>
    [Generator]
    public class mROAGenerator : IIncrementalGenerator
    {
        private static readonly Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        private static readonly Predicate<ITypeSymbol> ParameterFilterForType =
            i => i.Name is "CancellationToken" or "RequestContext";

        private TemplateDocument _indexerTemplate;
        private TemplateDocument _binderTemplate;
        private TemplateDocument _classTemplate;
        private TemplateDocument _classTemplateOriginal;
        private TemplateDocument _interfaceTemplate;
        private TemplateDocument _interfaceTemplateOriginal;
        private TemplateDocument _methodInvokerOriginal;
        private TemplateDocument _methodRepoTemplate;
        private int _currentInternalCallIndex;


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            _methodRepoTemplate = TemplateReader.FromEmbeddedResource("MethodRepo.cstmpl");
            _methodInvokerOriginal =
                ((InnerTemplateSection)_methodRepoTemplate["syncInvoker"]!).InnerTemplate;
            _classTemplateOriginal = TemplateReader.FromEmbeddedResource("Proxy.cstmpl");
            _binderTemplate = TemplateReader.FromEmbeddedResource("RemoteTypeBinder.cstmpl");
            _interfaceTemplateOriginal = TemplateReader.FromEmbeddedResource("PartialInterface.cstmpl");
            _indexerTemplate = TemplateReader.FromEmbeddedResource("IndexProvider.cstmpl");

            var syntaxes = context.SyntaxProvider.CreateSyntaxProvider(
                    (static (node, _) => node is InterfaceDeclarationSyntax),
                    static (node, _) => CodegenUtilities.ContainsSoiAttribute(node)).Where(i => i.usefull)
                .Select((node, _) => node.node);

            context.RegisterSourceOutput(context.CompilationProvider.Combine(syntaxes.Collect()),
                (productionContext, pair) => GenerateCode(productionContext, pair.Left, pair.Right));
        }

        private void GenerateCode(SourceProductionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
            var totalMethods = new List<IMethodSymbol>();

            _indexerTemplate.AddDefine("namespace", compilation.AssemblyName!);
            var invokers = new List<string>();
            var declarations = classes.ToList();
            var apiLevel = 0;
            foreach (var classDeclarationSyntax in declarations)
            {
                _currentInternalCallIndex = 0;
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classDeclarationSyntax.Identifier.Text;
                var innerMethods = CodegenUtilities.CollectMembers(classSymbol);
                var associated = innerMethods.Select(i => i.AssociatedSymbol).Where(i => i != null)
                    .Distinct(SymbolEqualityComparer.Default).Cast<ISymbol>().ToList();

                innerMethods = innerMethods.Where(i =>
                        i.MethodKind != MethodKind.EventAdd && i.MethodKind != MethodKind.EventRemove)
                    .ToList();

                totalMethods.AddRange(innerMethods);

                var originalName = className;

                className = className.TrimStart('I') + "Proxy";

                _classTemplate = (TemplateDocument)_classTemplateOriginal.Clone();
                var propertiesAccessMethods = new List<(string, IMethodSymbol)>();

                int startInvokers = invokers.Count;
                foreach (var method in innerMethods)
                    switch (method.MethodKind)
                    {
                        case MethodKind.PropertyGet or MethodKind.PropertySet:
                            GeneratePropertyMethod(method, propertiesAccessMethods, invokers,
                                classSymbol);
                            continue;
                        default:
                            GenerateDeclaredMethod(method, invokers, classSymbol);
                            break;
                    }

                foreach (var symbol in associated)
                    switch (symbol)
                    {
                        case IPropertySymbol propertySymbol:

                            var setter = propertiesAccessMethods.FirstOrDefault(i =>
                                i.Item2.AssociatedSymbol!.Name == propertySymbol.Name && i.Item2.ReturnsVoid);
                            var getter = propertiesAccessMethods.FirstOrDefault(i =>
                                i.Item2.AssociatedSymbol!.Name == propertySymbol.Name && !i.Item2.ReturnsVoid);
                            string impl;
                            if (propertySymbol.IsIndexer)
                                impl =
                                    $"public {propertySymbol.Type.ToUnityString()} this[{string.Join(", ", propertySymbol.Parameters.Select(p => p.ToUnityString()))}] {{ {getter.Item1} {setter.Item1} }}";
                            else
                                impl =
                                    $"public {propertySymbol.Type.ToUnityString()} {symbol.Name} {{ {getter.Item1} {setter.Item1} }}";

                            _classTemplate.Insert("methods", impl);
                            // declaredMethods.Add(impl);
                            break;
                        case IEventSymbol eventSymbol:
                            _classTemplate.Insert("methods",
                                $"public event {eventSymbol.Type.ToUnityString()}? {eventSymbol.Name};");
                            // declaredMethods.Add(
                            //     $"public event {eventSymbol.Type.ToDisplayString()}? {eventSymbol.Name};");
                            break;
                    }

                GenerateEventImplementation(classSymbol, invokers, context);
                var endInvokers = invokers.Count;
                _indexerTemplate.Insert("indexSpan",
                    $"{{ typeof({originalName}), new[] {{ {CodegenUtilities.JoinWithComa(Enumerable.Range(startInvokers, endInvokers - startInvokers).Select(i => i.ToString()))} }} }},");
                _classTemplate.AddDefine("className", className);
                _classTemplate.AddDefine("originalName", originalName);
                _classTemplate.AddDefine("namespaceName", namespaceName);

                var code = _classTemplate.Compile();


                // Add the source code to the compilation.
#if !DONT_ADD
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                _indexerTemplate.Insert("remoteTypePair",
                    $"{{ typeof({classSymbol.ToUnityString()}), (id, r, c, indices) => new {namespaceName}.{className}(id, r, c, indices) }}");
            }

            if (totalMethods.Count != 0)
            {
                var coCodegenRepoCode = _methodRepoTemplate.Compile();
                _indexerTemplate.AddDefine("level", apiLevel.ToString());
                _indexerTemplate.AddDefine("len", invokers.Count.ToString());

                var providerCode = _indexerTemplate.Compile();
#if !DONT_ADD
                context.AddSource("GeneratedInvokersCollection.g.cs",
                    SourceText.From(coCodegenRepoCode, Encoding.UTF8));
                context.AddSource("GeneratedIndexProvider.g.cs", SourceText.From(providerCode, Encoding.UTF8));

#endif
            }

            if (_indexerTemplate["remoteTypePair+"] != null)
            {
                var frontendRepoCode = _binderTemplate.Compile();
#if !DONT_ADD
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(frontendRepoCode, Encoding.UTF8));
#endif
            }
        }

        private void GenerateEventImplementation(INamedTypeSymbol classSymbol, List<string> invokers,
            SourceProductionContext context)
        {
            var events = classSymbol.AllInterfaces.Add(classSymbol).SelectMany(i => i.GetMembers())
                .OfType<IEventSymbol>().ToList();
            if (events.Count == 0)
                return;

            _interfaceTemplate = (TemplateDocument)_interfaceTemplateOriginal.Clone();
            _interfaceTemplate.AddDefine("name", classSymbol.Name);
            _interfaceTemplate.AddDefine("namespace", classSymbol.ContainingNamespace.ToDisplayString());
            var objectBinderTemplate =
                (TemplateDocument)((InnerTemplateSection)_binderTemplate["objectBinderTemplate"]!).InnerTemplate
                .Clone();
            foreach (var currentEvent in events)
            {
                var additionalMethod = GenerateMethodExternalCaller(currentEvent, out var signature);
                _classTemplate.Insert("methods", additionalMethod);

                // declaredMethods.Add(additionalMethod);
                _interfaceTemplate.Insert("signature", signature);
                GenerateEventCode(currentEvent, invokers, classSymbol);
                GenerateBinderCode(currentEvent, classSymbol, objectBinderTemplate);
            }

            objectBinderTemplate.AddDefine("type", classSymbol.ToUnityString());
            var partialInterface = _interfaceTemplate.Compile();
            var binder = objectBinderTemplate.Compile();
            _binderTemplate.Insert("eventBinder", binder);

#if !DONT_ADD
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(partialInterface, Encoding.UTF8));
#endif
        }

        private string GenerateMethodExternalCaller(IEventSymbol eventSymbol, out string interfaceSignature)
        {
            const string level = "\t\t";
            var parameters = ((INamedTypeSymbol)eventSymbol.Type).TypeArguments;
            var parameterIndex = 0;
            var parametersDeclaration =
                string.Join(", ", parameters.Select(i => $"{i.ToUnityString()} p{parameterIndex++}"));
            var signature = $"public void {CodegenUtilities.EventExternalName(eventSymbol)}({parametersDeclaration})";
            interfaceSignature = signature + ";";
            var caller = $@"{signature}
{level}{{
{level}    {eventSymbol.Name}?.Invoke({string.Join(", ", Enumerable.Range(0, parameterIndex).Select(i => "p" + i))});
{level}}} 
";
            return caller;
        }

        private void GenerateDeclaredMethod(IMethodSymbol method, List<string> invokers, INamedTypeSymbol baseInterface)
        {
            var sb = new StringBuilder();

            bool isParametrized;

            bool isAsync;
            bool isVoid;

            List<IParameterSymbol>? parameters;

            switch (method.ReturnType)
            {
                case INamedTypeSymbol namedType:
                    isAsync = namedType.Name == "Task";
                    isVoid = (isAsync && namedType.TypeParameters.Length == 0) || namedType.Name == "Void";
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
                          $"{method.ReturnType.ToUnityString()} {method.Name}({string.Join(", ", method.Parameters.Select(CodegenUtilities.ToFullString))}){{");

            var isUntrusted = method.GetAttributes().Any(i => i.AttributeClass?.Name == "UntrustedAttribute");

            var prefix = isAsync ? "await " : "";
            var postfix = !isAsync ? isVoid ? ".Wait()" : ".GetAwaiter().GetResult()" : "";
            var parameterLink = isParametrized
                ? ", new System.Object[] { " + string.Join(", ", parameters.Select(i => i.Name)) + " }"
                : string.Empty;

            string caller;

            if (isUntrusted)
            {
                caller = $"CallUntrustedAsync(CallIndices[{_currentInternalCallIndex++}]{parameterLink})";
            }
            else
            {
                var tokenInsert = isAsync &&
                                  method.Parameters.FirstOrDefault(i => i.Type.Name == "CancellationToken") is
                                      { } tokenSymbol
                    ? ", cancellationToken : " + tokenSymbol.Name
                    : string.Empty;

                caller = isVoid
                    ? $"CallAsync(CallIndices[{_currentInternalCallIndex++}]{parameterLink}{tokenInsert})"
                    : isAsync
                        ? $"GetResultAsync<{CodegenUtilities.ExtractTaskType(method.ReturnType)}>(CallIndices[{_currentInternalCallIndex++}]{parameterLink}{tokenInsert})"
                        : $"GetResultAsync<{CodegenUtilities.ToFullString(method.ReturnType)}>(CallIndices[{_currentInternalCallIndex++}]{parameterLink}{tokenInsert})";

                if (!isVoid)
                    prefix = "return " + prefix;
            }

            sb.AppendLine("\t\t\t" + prefix + caller + postfix + ";");

            sb.AppendLine("\t\t}");
            _classTemplate.Insert("methods", sb.ToString());

            // declaredMethods.Add(sb.ToString());
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parameters.Select(p => $"typeof({p.Type.ToUnityString()})"))}");
            var level = "\t\t\t";

            var parametersInsertList = new List<string>();

            foreach (var parameter in method.Parameters)
                switch (parameter.Type.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("special[0] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(CodegenUtilities.Caster(parameter.Type,
                            $"parameters[{parameters.IndexOf(parameter)}]"));
                        break;
                }

            var parametersInsert = string.Join(", ", parametersInsertList);
            string backend;

            string funcInvoking;

            if (isAsync && !isVoid)
                funcInvoking =
                    $"(i as {method.ContainingType.ToUnityString()}).{method.Name}({parametersInsert}).ContinueWith(t => {{ post(t.Result); }})";
            else if (isAsync && isVoid)
                funcInvoking =
                    $"(i as {method.ContainingType.ToUnityString()}).{method.Name}({parametersInsert}).ContinueWith(t => {{ post(null); }})";
            else if (!isAsync && isVoid)
                funcInvoking = $@"{{
{level}        (i as {method.ContainingType.ToUnityString()}).{method.Name}({parametersInsert});
{level}        return null;
{level}    }}";
            else
                funcInvoking = $"(i as {method.ContainingType.ToUnityString()}).{method.Name}({parametersInsert})";

            if (isAsync)
            {
                var invokerTemplate =
                    (TemplateDocument)((InnerTemplateSection)_methodRepoTemplate["asyncInvoker"]!).InnerTemplate
                    .Clone();
                invokerTemplate.AddDefine("isVoid", isVoid.ToString().ToLower());
                invokerTemplate.AddDefine("returnType", CodegenUtilities.ExtractTaskType(method.ReturnType));
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterface.ToUnityString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                invokerTemplate.AddDefine("isTrusted", (!isUntrusted).ToString().ToLower());
                backend = invokerTemplate.Compile();
            }
            else
            {
                var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
                invokerTemplate.AddDefine("isVoid", isVoid.ToString().ToLower());
                invokerTemplate.AddDefine("returnType", isVoid ? "void" : method.ReturnType.ToUnityString());
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterface.ToUnityString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                invokerTemplate.AddDefine("isTrusted", (!isUntrusted).ToString().ToLower());
                backend = invokerTemplate.Compile();
            }

            _methodRepoTemplate.Insert("invoker", backend);

            invokers.Add(backend);
        }

        private void GenerateBinderCode(IEventSymbol eventSymbol, INamedTypeSymbol baseType, TemplateDocument document)
        {
            var eventBinderTemplate =
                (TemplateDocument)((InnerTemplateSection)document["eventBinderTemplate"]!).InnerTemplate.Clone();

            var index = _currentInternalCallIndex;
            var parameters = (eventSymbol.Type as INamedTypeSymbol)!.TypeArguments.ToList();
            var parametersDeclaration = string.Join(", ",
                CodegenUtilities.JoinWithComa(Enumerable.Range(0, parameters.Count).Select(i => "p" + i)));


            var pi = 0;
            var transferParameters = CodegenUtilities.JoinWithComa(parameters.Select(i => (i, pi++))
                .Where(i => !ParameterFilterForType(i.i))
                .Select(i => "p" + i.Item2));

            var requestIndex = parameters.FindIndex(i => i.Name == "RequestContext");
            if (requestIndex != -1)
            {
                var callFilter = $"\n\r\t\t\tif(ownerId == p{requestIndex}.OwnerId) return;";
                eventBinderTemplate.AddDefine("callFilter", callFilter);
            }

            eventBinderTemplate.AddDefine("type", baseType.ToUnityString());
            eventBinderTemplate.AddDefine("eventName", eventSymbol.Name);
            eventBinderTemplate.AddDefine("parametersDeclaration", parametersDeclaration);
            eventBinderTemplate.AddDefine("commandId",
                $"context.CallIndexProvider.GetIndices(typeof({baseType.ToUnityString()}))[{index}]");
            eventBinderTemplate.AddDefine("transferParameters", transferParameters);
            var eventBinderCode = eventBinderTemplate.Compile();
            document.Insert("eventBinder", eventBinderCode);
        }

        private void GenerateEventCode(IEventSymbol eventSymbol, List<string> invokers, ITypeSymbol baseInterface)
        {
            var level = "\t\t\t";

            int pi = 0;
            var parameters = ((INamedTypeSymbol)eventSymbol.Type).TypeArguments.Select(i => (i, pi++))
                .ToImmutableArray();
            var parsingParameters = parameters.RemoveAll(i => ParameterFilterForType(i.i)).ToList();
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parsingParameters.Select(p => $"typeof({p.i.ToUnityString()})"))}");

            var parametersInsertList = new List<string>();

            foreach (var parameter in parameters)
                switch (parameter.i.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("special[0] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(CodegenUtilities.Caster(parameter.i,
                            $"parameters[{parameter.Item2}]"));
                        break;
                }


            var parametersInsert = string.Join(", ", parametersInsertList);

            var funcInvoking = $@"{{
{level}        (i as {baseInterface.ToUnityString()}).{CodegenUtilities.EventExternalName(eventSymbol)}({parametersInsert});
{level}        return null;
{level}    }}";

            var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
            invokerTemplate.AddDefine("isVoid", "true");
            invokerTemplate.AddDefine("returnType", "void");
            invokerTemplate.AddDefine("parametersType", parameterTypes);
            invokerTemplate.AddDefine("suitableType", baseInterface.ToUnityString());
            invokerTemplate.AddDefine("funcInvoking", funcInvoking);
            invokerTemplate.AddDefine("isTrusted", "true");
            var backend = invokerTemplate.Compile();
            _methodRepoTemplate.Insert("invoker", backend);

            invokers.Add(backend);
        }

        private void GeneratePropertyMethod(IMethodSymbol method,
            List<(string, IMethodSymbol)> propsCollection, List<string> invokers, INamedTypeSymbol baseInterace)
        {
            string frontend;
            string backend;
            if (method.MethodKind == MethodKind.PropertyGet)
            {
                var parametersArray = "";
                if (method.Parameters.Length != 0)
                {
                    parametersArray =
                        $", new System.Object[] {{ {string.Join(", ", method.Parameters.Select(p => p.Name))} }}";

                    var parameterTypes = string.Join(", ",
                        $"{string.Join(", ", method.Parameters.Select(p => "typeof(" + p.Type.ToUnityString() + ")"))}");
                    var parameterInserts = string.Join(", ",
                        method.Parameters.Select(p =>
                            CodegenUtilities.Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));

                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
                    invokerTemplate.AddDefine("isVoid", "false");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}]");
                    invokerTemplate.AddDefine("isTrusted", "true");

                    backend = invokerTemplate.Compile();
                }
                else
                {
                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
                    invokerTemplate.AddDefine("isVoid", "false");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name}");
                    invokerTemplate.AddDefine("isTrusted", "true");

                    backend = invokerTemplate.Compile();
                }

                frontend =
                    $"get => GetResultAsync<{method.ReturnType.ToUnityString()}>(CallIndices[{_currentInternalCallIndex++}]{parametersArray}).GetAwaiter().GetResult();";
            }
            else
            {
                var parametersArray = "value";
                if (method.Parameters.Length != 1)
                {
                    parametersArray = string.Join(", ", method.Parameters.Select(p => p.Name));

                    var parameterTypes = string.Join(", ",
                        $"{string.Join(", ", method.Parameters.Select(p => $"typeof({p.Type.ToUnityString()})"))}");
                    var parameterInserts = string.Join(", ",
                        method.Parameters.Take(method.Parameters.Length - 1).Select(p =>
                            CodegenUtilities.Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));

                    var valueInsert = CodegenUtilities.Caster(method.Parameters.Last().Type,
                        "parameters[" + (method.Parameters.Length - 1) + "]");
                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();


                    invokerTemplate.AddDefine("isVoid", "true");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}] = {valueInsert}");
                    invokerTemplate.AddDefine("isTrusted", "true");

                    backend = invokerTemplate.Compile();
                }
                else
                {
                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();

                    invokerTemplate.AddDefine("isVoid", "true");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("parametersType",
                        $"typeof({method.Parameters.First().Type.ToUnityString()})");
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {CodegenUtilities.Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")}");
                    invokerTemplate.AddDefine("isTrusted", "true");

                    backend = invokerTemplate.Compile();
                }

                frontend =
                    $"set => CallAsync(CallIndices[{_currentInternalCallIndex++}], new System.Object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((frontend, method));
            _methodRepoTemplate.Insert("invoker", backend);
            invokers.Add(backend);
        }
    }

    public static class CodegenUtilities
    {
        public static string EventExternalName(IEventSymbol eventSymbol)
        {
            return $"{eventSymbol.Name}External";
        }

        public static string Caster(ITypeSymbol type, string inner)
        {
            if (!type.IsValueType)
                return inner +
                       " as " +
                       type.ToUnityString();
            return $"({type.ToUnityString()})" + inner;
        }

        public static string JoinWithComa(IEnumerable<string> parts)
        {
            return string.Join(", ", parts);
        }

        public static string ToFullString(IParameterSymbol parameter)
        {
            return parameter.Type.ToUnityString() + " " + parameter.Name;
        }

        public static string ToFullString(ITypeSymbol type)
        {
            return type.ToUnityString();
        }

        public static string ExtractTaskType(ITypeSymbol taskType)
        {
            var generics = ((INamedTypeSymbol)taskType).TypeArguments;
            return generics.Length == 0 ? "void" : generics[0].ToUnityString();
        }

        public static (InterfaceDeclarationSyntax node, bool usefull) ContainsSoiAttribute(
            GeneratorSyntaxContext context)
        {
            var ids = (InterfaceDeclarationSyntax)context.Node;

            // Go through all attributes of the class.
            foreach (var attributeSyntax in ids.AttributeLists.SelectMany(attributeListSyntax =>
                         attributeListSyntax.Attributes))
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue; // if we can't get the symbol, ignore it

                var attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the [Report] attribute.
                if (attributeName == $"mROA.Implementation.Attributes.SharedObjectInterfaceAttribute")
                    return (ids, true);
            }

            return (ids, false);
        }

        public static List<IMethodSymbol> CollectMembers(INamedTypeSymbol type)
        {
            var methods = type.GetMembers().OfType<IMethodSymbol>().ToList();
            foreach (var inner in type.AllInterfaces) methods.AddRange(inner.GetMembers().OfType<IMethodSymbol>());

            methods.RemoveAll(m => m.Name == "Dispose");
            return methods.OrderBy(i => i.Name).ToList();
        }
    }

    public static class CodegenExtensions
    {
        public static string ToUnityString(this ITypeSymbol type)
        {
            var parts = type.ToDisplayParts();

            return parts.Any(i => i.Kind == SymbolDisplayPartKind.Keyword)
                ? parts.ToUnityString()
                : type.ToDisplayString();
        }

        public static string ToUnityString(this IParameterSymbol parameter)
        {
            return parameter.Type.ToUnityString() + " " + parameter.Name;
        }

        private static string ToUnityString(this ImmutableArray<SymbolDisplayPart> parts)
        {
            var sb = new StringBuilder();
            foreach (var displayPart in parts)
                if (displayPart.Kind == SymbolDisplayPartKind.Keyword)
                {
                    if (displayPart.ToString() == "void")
                    {
                        sb.Append(displayPart.ToString());
                        continue;
                    }

                    sb.Append($"{displayPart.Symbol!.ContainingNamespace.ToDisplayString()}.{displayPart.Symbol.Name}");
                }
                else
                {
                    sb.Append(displayPart.ToString());
                }

            return sb.ToString();
        }
    }
}