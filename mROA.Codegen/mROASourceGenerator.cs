// #define DONT_ADD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using mROA.Codegen.Templates;


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
        private const string SharedObjectInterfaceAttributeName = "SharedObjectInterface";

        private static readonly Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        private static readonly Predicate<ITypeSymbol> ParameterFilterForType =
            i => i.Name is "CancellationToken" or "RequestContext";
        
        private int _currentInternalCallIndex;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxes = context.SyntaxProvider.CreateSyntaxProvider(
                NodeIsInterfaceWithSharedObjectInterfaceAttribute,
                TransformToInterfaceDeclarationSyntax);

            var incrementalValueProvider = context.CompilationProvider.Combine(syntaxes.Collect());
            context.RegisterSourceOutput(incrementalValueProvider,
                (productionContext, pair) => GenerateCode(productionContext, pair.Left, pair.Right));
        }

        private static InterfaceDeclarationSyntax TransformToInterfaceDeclarationSyntax(GeneratorSyntaxContext context,
            CancellationToken _)
        {
            if (context.Node is not InterfaceDeclarationSyntax interfaceSyntax)
                throw new InvalidOperationException();
            return interfaceSyntax;
        }

        private static bool NodeIsInterfaceWithSharedObjectInterfaceAttribute(SyntaxNode node, CancellationToken _)
        {
            if (node is not InterfaceDeclarationSyntax interfaceSyntax)
                return false;

            var attributes = interfaceSyntax.AttributeLists.SelectMany(list => list.Attributes);
            return attributes.Any(attribute => attribute.ToFullString() == SharedObjectInterfaceAttributeName);
        }

        private void GenerateCode(SourceProductionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
            var methodRepoTemplate = new MethodRepoTemplate();
            var typeBinder = new RemoteTypeBinderTemplate();
            var indexProviderTemplate = new IndexProviderTemplate();
            var totalMethods = new List<IMethodSymbol>();

            if (compilation.AssemblyName == null)
                throw new InvalidOperationException();
            
            indexProviderTemplate.DefineNamespace(compilation.AssemblyName);
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

                var proxyTemplate = new ProxyTemplate();
                var propertiesAccessMethods = new List<(string, IMethodSymbol)>();

                int startInvokers = invokers.Count;
                foreach (var method in innerMethods)
                    switch (method.MethodKind)
                    {
                        case MethodKind.PropertyGet or MethodKind.PropertySet:
                            GeneratePropertyMethod(methodRepoTemplate, method, propertiesAccessMethods, invokers,
                                classSymbol);
                            continue;
                        default:
                            GenerateDeclaredMethod(proxyTemplate, methodRepoTemplate, method, invokers, classSymbol);
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

                            proxyTemplate.InsertMethods(impl);
                            // declaredMethods.Add(impl);
                            break;
                        case IEventSymbol eventSymbol:
                            proxyTemplate.InsertMethods($"public event {eventSymbol.Type.ToUnityString()}? {eventSymbol.Name};");
                            // declaredMethods.Add(
                            //     $"public event {eventSymbol.Type.ToDisplayString()}? {eventSymbol.Name};");
                            break;
                    }

                GenerateEventImplementation(proxyTemplate, methodRepoTemplate, typeBinder, classSymbol, invokers, context);
                var endInvokers = invokers.Count;
                indexProviderTemplate.InsertIndexSpan($"{{ typeof({originalName}), new[] {{ {CodegenUtilities.JoinWithComa(Enumerable.Range(startInvokers, endInvokers - startInvokers).Select(i => i.ToString()))} }} }},");
                proxyTemplate.DefineClassName(className);
                proxyTemplate.DefineOriginalName(originalName);
                proxyTemplate.DefineNamespaceName(namespaceName);

                var code = proxyTemplate.Compile();


                // Add the source code to the compilation.
#if !DONT_ADD
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                indexProviderTemplate.InsertRemoteTypePair($"{{ typeof({classSymbol.ToUnityString()}), (id, r, c, indices) => new {namespaceName}.{className}(id, r, c, indices) }}");
            }

            if (totalMethods.Count != 0)
            {
                var coCodegenRepoCode = methodRepoTemplate.Compile();
                indexProviderTemplate.DefineLevel(apiLevel.ToString());
                indexProviderTemplate.DefineLen(invokers.Count.ToString());

                var providerCode = indexProviderTemplate.Compile();
#if !DONT_ADD
                context.AddSource("GeneratedInvokersCollection.g.cs",
                    SourceText.From(coCodegenRepoCode, Encoding.UTF8));
                context.AddSource("GeneratedIndexProvider.g.cs", SourceText.From(providerCode, Encoding.UTF8));

#endif
            }

            if (indexProviderTemplate.IsRemoteTypePairInserted())
            {
                var frontendRepoCode = typeBinder.Compile();
#if !DONT_ADD
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(frontendRepoCode, Encoding.UTF8));
#endif
            }
        }

        private void GenerateEventImplementation(ProxyTemplate proxyTemplate, MethodRepoTemplate methodRepoTemplate, RemoteTypeBinderTemplate remoteTypeBinder, INamedTypeSymbol classSymbol,
            List<string> invokers, SourceProductionContext context)
        {
            var events = classSymbol.AllInterfaces.Add(classSymbol).SelectMany(i => i.GetMembers())
                .OfType<IEventSymbol>().ToList();
            if (events.Count == 0)
                return;

            var partialInterfaceTemplate = new PartialInterfaceTemplate();
            partialInterfaceTemplate.DefineName(classSymbol.Name);
            partialInterfaceTemplate.DefineNamespace(classSymbol.ContainingNamespace.ToDisplayString());
            
            var objectBinderTemplate = remoteTypeBinder.CloneInnerObjectBinder();
            foreach (var currentEvent in events)
            {
                var additionalMethod = GenerateMethodExternalCaller(currentEvent, out var signature);
                proxyTemplate.InsertMethods(additionalMethod);

                // declaredMethods.Add(additionalMethod);
                partialInterfaceTemplate.InsertSignature(signature);
                GenerateEventCode(methodRepoTemplate, currentEvent, invokers, classSymbol);
                GenerateBinderCode(objectBinderTemplate, currentEvent, classSymbol);
            }
            
            objectBinderTemplate.DefineType(classSymbol.ToUnityString());
            var partialInterface = partialInterfaceTemplate.Compile();
            var binder = objectBinderTemplate.Compile();
            remoteTypeBinder.InsertEventBinder(binder);

#if !DONT_ADD
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(partialInterface, Encoding.UTF8));
#endif
        }

        private static string GenerateMethodExternalCaller(IEventSymbol eventSymbol, out string interfaceSignature)
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

        private void GenerateDeclaredMethod(ProxyTemplate proxyTemplate, MethodRepoTemplate methodRepoTemplate, IMethodSymbol method, List<string> invokers,
            INamedTypeSymbol baseInterface)
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
            proxyTemplate.InsertMethods(sb.ToString());

            // declaredMethods.Add(sb.ToString());
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parameters.Select(p => $"typeof({p.Type.ToUnityString()})"))}");
            var level = "\t\t\t";

            var parametersInsertList = new List<string>();

            var useCancellationToken = false;
            
            foreach (var parameter in method.Parameters)
                switch (parameter.Type.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        useCancellationToken = true;
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("(RequestContext)special[0]");
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
                var invokerTemplate = methodRepoTemplate.CloneInnerAsyncInvoker();
                invokerTemplate.DefineIsVoid(isVoid.ToString().ToLower());
                invokerTemplate.DefineReturnType(CodegenUtilities.ExtractTaskType(method.ReturnType));
                invokerTemplate.DefineParametersType(parameterTypes);
                invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                invokerTemplate.DefineFuncInvoking(funcInvoking);
                invokerTemplate.DefineIsTrusted((!isUntrusted).ToString().ToLower());
                invokerTemplate.DefineCancellation(useCancellationToken.ToString().ToLower());
                backend = invokerTemplate.Compile();
            }
            else
            {
                var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
                invokerTemplate.DefineIsVoid(isVoid.ToString().ToLower());
                invokerTemplate.DefineReturnType(isVoid ? "void" : method.ReturnType.ToUnityString());
                invokerTemplate.DefineParametersType(parameterTypes);
                invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                invokerTemplate.DefineFuncInvoking(funcInvoking);
                invokerTemplate.DefineIsTrusted((!isUntrusted).ToString().ToLower());
                
                backend = invokerTemplate.Compile();
            }

            methodRepoTemplate.InsertInvoke(backend);

            invokers.Add(backend);
        }

        private void GenerateBinderCode(ObjectBinderTemplate objectBinderTemplate, IEventSymbol eventSymbol, INamedTypeSymbol baseType)
        {
            var eventBinderTemplate = objectBinderTemplate.CloneInnerEventBinder();

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
                eventBinderTemplate.DefineCallFilter(callFilter);
            }

            eventBinderTemplate.DefineType(baseType.ToUnityString());
            eventBinderTemplate.DefineEventName(eventSymbol.Name);
            eventBinderTemplate.DefineParametersDeclaration(parametersDeclaration);
            eventBinderTemplate.DefineCommandIdTag($"context.CallIndexProvider.GetIndices(typeof({baseType.ToUnityString()}))[{index}]");
            eventBinderTemplate.DefineTransferParameters(transferParameters);
            var eventBinderCode = eventBinderTemplate.Compile();
            objectBinderTemplate.InsertEventBinder(eventBinderCode);
        }

        private void GenerateEventCode(MethodRepoTemplate methodRepoTemplate, IEventSymbol eventSymbol, List<string> invokers, ITypeSymbol baseInterface)
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
                        parametersInsertList.Add("(RequestContext)special[0]");
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

            var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
            invokerTemplate.DefineIsVoid("true");
            invokerTemplate.DefineReturnType("void");
            invokerTemplate.DefineParametersType(parameterTypes);
            invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
            invokerTemplate.DefineFuncInvoking(funcInvoking);
            invokerTemplate.DefineIsTrusted("true");
            var backend = invokerTemplate.Compile();
            methodRepoTemplate.InsertInvoke(backend);

            invokers.Add(backend);
        }

        private void GeneratePropertyMethod(MethodRepoTemplate methodRepoTemplate, IMethodSymbol method,
            List<(string, IMethodSymbol)> propsCollection, List<string> invokers, INamedTypeSymbol baseInterface)
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

                    var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
                    invokerTemplate.DefineIsVoid("false");
                    invokerTemplate.DefineReturnType(method.ReturnType.ToUnityString());
                    invokerTemplate.DefineParametersType(parameterTypes);
                    invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                    invokerTemplate.DefineFuncInvoking($"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}]");
                    invokerTemplate.DefineIsTrusted("true");

                    backend = invokerTemplate.Compile();
                }
                else
                {
                    var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
                    invokerTemplate.DefineIsVoid("false");
                    invokerTemplate.DefineReturnType(method.ReturnType.ToUnityString());
                    invokerTemplate.DefineParametersType("");
                    invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                    invokerTemplate.DefineFuncInvoking($"(i as {method.ContainingType.ToUnityString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name}");
                    invokerTemplate.DefineIsTrusted("true");

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
                    
                    var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
                    invokerTemplate.DefineIsVoid("true");
                    invokerTemplate.DefineReturnType(method.ReturnType.ToUnityString());
                    invokerTemplate.DefineParametersType(parameterTypes);
                    invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                    invokerTemplate.DefineFuncInvoking($"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}] = {valueInsert}");
                    invokerTemplate.DefineIsTrusted("true");

                    backend = invokerTemplate.Compile();
                }
                else
                {
                    var invokerTemplate = methodRepoTemplate.CloneInnerSyncInvoker();
                    invokerTemplate.DefineIsVoid("true");
                    invokerTemplate.DefineReturnType(method.ReturnType.ToUnityString());
                    invokerTemplate.DefineParametersType($"typeof({method.Parameters.First().Type.ToUnityString()})");
                    invokerTemplate.DefineSuitableType(baseInterface.ToUnityString());
                    invokerTemplate.DefineFuncInvoking($"(i as {method.ContainingType.ToUnityString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {CodegenUtilities.Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")}");
                    invokerTemplate.DefineIsTrusted("true");

                    backend = invokerTemplate.Compile();
                }

                frontend =
                    $"set => CallAsync(CallIndices[{_currentInternalCallIndex++}], new System.Object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((frontend, method));
            methodRepoTemplate.InsertInvoke(backend);
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