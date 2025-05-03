// #define DONT_ADD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using mROA.Codegen.Tools;
using mROA.Codegen.Tools.Reading;

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
    public class mROAGenerator : ISourceGenerator
    {
        private static readonly Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        private static readonly Predicate<ITypeSymbol> ParameterFilterForType =
            i => i.Name is "CancellationToken" or "RequestContext";

        private TemplateDocument _binderTemplate;
        private TemplateDocument _classTemplate;
        private TemplateDocument _classTemplateOriginal;
        private TemplateDocument _interfaceTemplate;
        private TemplateDocument _interfaceTemplateOriginal;
        private TemplateDocument _methodInvokerOriginal;
        private TemplateDocument _methodRepoTemplate;

        public void Initialize(GeneratorInitializationContext context)
        {
            _methodRepoTemplate = TemplateReader.FromEmbeddedResource("MethodRepo.cstmpl");
            _methodInvokerOriginal =
                ((InnerTemplateSection)_methodRepoTemplate["syncInvoker"]!).InnerTemplate;
            _classTemplateOriginal = TemplateReader.FromEmbeddedResource("RemoteEndpoint.cstmpl");
            _binderTemplate = TemplateReader.FromEmbeddedResource("RemoteTypeBinder.cstmpl");
            _interfaceTemplateOriginal = TemplateReader.FromEmbeddedResource("PartialInterface.cstmpl");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var trees = context.Compilation.SyntaxTrees;

                var interfaces = new List<InterfaceDeclarationSyntax>();
                foreach (var tree in trees)
                {
                    var node = (CompilationUnitSyntax)tree.GetRoot();

                    foreach (var member in node.Members)
                        if (member is InterfaceDeclarationSyntax ids)
                            interfaces.Add(ids);
                        else if (member is NamespaceDeclarationSyntax nds)
                            foreach (var inside in nds.Members)

                                if (inside is InterfaceDeclarationSyntax ids2)
                                    if (ContainsSoiAttribute(ids2.AttributeLists, context, ids2))
                                        interfaces.Add(ids2);
                }

                GenerateCode(context, context.Compilation, interfaces.ToImmutableArray());
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR: Unable to load method repository");
            }
        }

        private void GenerateCode(GeneratorExecutionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
            var totalMethods = new List<IMethodSymbol>();


            var invokers = new List<string>();
            var declarations = classes.ToList().OrderBy(i => i.Identifier.Text).ToList();
            foreach (var classDeclarationSyntax in declarations)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                    continue;

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classDeclarationSyntax.Identifier.Text;
                var innerMethods = CollectMembers(classSymbol);
                var associated = innerMethods.Select(i => i.AssociatedSymbol).Where(i => i != null)
                    .Distinct(SymbolEqualityComparer.Default).Cast<ISymbol>().ToList();

                innerMethods = innerMethods.Where(i =>
                        i.MethodKind != MethodKind.EventAdd && i.MethodKind != MethodKind.EventRemove)
                    .ToList();

                totalMethods.AddRange(innerMethods);

                var originalName = className;

                className = className.TrimStart('I') + "RemoteEndpoint";

                _classTemplate = (TemplateDocument)_classTemplateOriginal.Clone();
                var propertiesAccessMethods = new List<(string, IMethodSymbol)>();

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

                _classTemplate.AddDefine("className", className);
                _classTemplate.AddDefine("originalName", originalName);
                _classTemplate.AddDefine("namespaceName", namespaceName);

                var code = _classTemplate.Compile();


                // Add the source code to the compilation.
#if !DONT_ADD
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                _binderTemplate.Insert("remoteTypePair",
                    $"{{ typeof({classSymbol.ToUnityString()}), typeof({namespaceName}.{className}) }}");
            }

            if (totalMethods.Count != 0)
            {
                var coCodegenRepoCode = _methodRepoTemplate.Compile();
#if !DONT_ADD
                context.AddSource("CoCodegenMethodRepository.g.cs", SourceText.From(coCodegenRepoCode, Encoding.UTF8));
#endif
            }

            if (_binderTemplate["remoteTypePair+"] != null)
            {
                var fronendRepoCode = _binderTemplate.Compile();
#if !DONT_ADD
                context.AddSource("RemoteTypeBinder.g.cs", SourceText.From(fronendRepoCode, Encoding.UTF8));
#endif
            }
        }

        private void GenerateEventImplementation(INamedTypeSymbol classSymbol, List<string> invokers,
            GeneratorExecutionContext context)
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
                GenerateBinderCode(currentEvent, invokers, classSymbol, objectBinderTemplate);
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
            var level = "\t\t";
            var parameters = ((INamedTypeSymbol)eventSymbol.Type).TypeArguments;
            var parameterIndex = 0;
            var parametersDeclaration =
                string.Join(", ", parameters.Select(i => $"{i.ToUnityString()} p{parameterIndex++}"));
            var signature = $@"public void {EventExternalName(eventSymbol)}({parametersDeclaration})";
            interfaceSignature = signature + ";";
            var caller = $@"{signature}
{level}{{
{level}    {eventSymbol.Name}?.Invoke({string.Join(", ", Enumerable.Range(0, parameterIndex).Select(i => "p" + i))});
{level}}} 
";
            return caller;
        }

        private static string EventExternalName(IEventSymbol eventSymbol)
        {
            return $"{eventSymbol.Name}External";
        }

        private static string Caster(ITypeSymbol type, string inner)
        {
            if (!type.IsValueType)
                return inner +
                       " as " +
                       type.ToUnityString();
            return $"({type.ToUnityString()})" + inner;
        }

        private void GenerateDeclaredMethod(IMethodSymbol method, List<string> invokers,
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
                          $"{method.ReturnType.ToUnityString()} {method.Name}({string.Join(", ", method.Parameters.Select(ToFullString))}){{");


            var isUntrusted = method.GetAttributes().Any(i => i.AttributeClass.Name == "UntrustedAttribute");

            var prefix = isAsync ? "await " : "";
            var postfix = !isAsync ? isVoid ? ".Wait()" : ".GetAwaiter().GetResult()" : "";
            var parameterLink = isParametrized
                ? ", new System.Object[] { " + string.Join(", ", parameters.Select(i => i.Name)) + " }"
                : string.Empty;

            string caller;

            if (isUntrusted)
            {
                caller = $"CallUntrustedAsync({index}{parameterLink})";
            }
            else
            {
                var tokenInsert = isAsync &&
                                  method.Parameters.FirstOrDefault(i => i.Type.Name == "CancellationToken") is
                                      { } tokenSymbol
                    ? ", cancellationToken : " + tokenSymbol.Name
                    : string.Empty;

                caller = isVoid
                    ? $"CallAsync({index}{parameterLink}{tokenInsert})"
                    : isAsync
                        ? $"GetResultAsync<{ExtractTaskType(method.ReturnType)}>({index}{parameterLink}{tokenInsert})"
                        : $"GetResultAsync<{ToFullString(method.ReturnType)}>({index}{parameterLink}{tokenInsert})";

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
                        parametersInsertList.Add(Caster(parameter.Type,
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
                invokerTemplate.AddDefine("returnType", ExtractTaskType(method.ReturnType));
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                backend = invokerTemplate.Compile();
            }
            else
            {
                var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
                invokerTemplate.AddDefine("isVoid", isVoid.ToString().ToLower());
                invokerTemplate.AddDefine("returnType", isVoid ? "void" : method.ReturnType.ToUnityString());
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                backend = invokerTemplate.Compile();
            }

            _methodRepoTemplate.Insert("invoker", backend);

            invokers.Add(backend);
        }

        private void GenerateBinderCode(IEventSymbol eventSymbol, List<string> invokers, INamedTypeSymbol baseType,
            TemplateDocument document)
        {
            var eventBinderTemplate =
                (TemplateDocument)((InnerTemplateSection)document["eventBinderTemplate"]!).InnerTemplate.Clone();

            var index = invokers.Count - 1;
            var parameters = (eventSymbol.Type as INamedTypeSymbol)!.TypeArguments.ToList();
            var parametersDeclaration = string.Join(", ",
                JoinWithComa(Enumerable.Range(0, parameters.Count).Select(i => "p" + i)));

            var transferParameters =
                JoinWithComa(parameters.Where(i => !ParameterFilterForType(i))
                    .Select(i => "p" + parameters.IndexOf(i)));

            var requestIndex = parameters.FindIndex(i => i.Name == "RequestContext");
            if (requestIndex != -1)
            {
                var callFilter = $"\n\r\t\t\tif(ownerId == p{requestIndex}.OwnerId) return;";
                eventBinderTemplate.AddDefine("callFilter", callFilter);
            }

            eventBinderTemplate.AddDefine("type", baseType.ToUnityString());
            eventBinderTemplate.AddDefine("eventName", eventSymbol.Name);
            eventBinderTemplate.AddDefine("parametersDeclaration", parametersDeclaration);
            eventBinderTemplate.AddDefine("commandId", index.ToString());
            eventBinderTemplate.AddDefine("transferParameters", transferParameters);
            var eventBinderCode = eventBinderTemplate.Compile();
            document.Insert("eventBinder", eventBinderCode);
        }

        private static string JoinWithComa(IEnumerable<string> parts)
        {
            return string.Join(", ", parts);
        }

        private void GenerateEventCode(IEventSymbol eventSymbol, List<string> invokers, ITypeSymbol baseInterface)
        {
            var level = "\t\t\t";

            var parameters = ((INamedTypeSymbol)eventSymbol.Type).TypeArguments;
            var parsingParameters = parameters.RemoveAll(ParameterFilterForType).ToList();
            var parameterTypes = string.Join(", ",
                $"{string.Join(", ", parsingParameters.Select(p => $"typeof({p.ToUnityString()})"))}");

            var parametersInsertList = new List<string>();

            foreach (var parameter in parameters)
                switch (parameter.Name)
                {
                    case "CancellationToken":
                        parametersInsertList.Add("(CancellationToken)special[1]");
                        break;
                    case "RequestContext":
                        parametersInsertList.Add("special[0] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(Caster(parameter,
                            $"parameters[{parameters.IndexOf(parameter)}]"));
                        break;
                }


            var parametersInsert = string.Join(", ", parametersInsertList);

            var funcInvoking = $@"{{
{level}        (i as {baseInterface.ToUnityString()}).{EventExternalName(eventSymbol)}({parametersInsert});
{level}        return null;
{level}    }}";

            var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
            invokerTemplate.AddDefine("isVoid", "true");
            invokerTemplate.AddDefine("returnType", "void");
            invokerTemplate.AddDefine("parametersType", parameterTypes);
            invokerTemplate.AddDefine("suitableType", baseInterface.ToUnityString());
            invokerTemplate.AddDefine("funcInvoking", funcInvoking);
            var backend = invokerTemplate.Compile();
            _methodRepoTemplate.Insert("invoker", backend);

            invokers.Add(backend);
        }

        private void GeneratePropertyMethod(IMethodSymbol method,
            List<(string, IMethodSymbol)> propsCollection, List<string> invokers, INamedTypeSymbol baseInterace)
        {
            var index = invokers.Count;
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
                            Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));

                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();
                    invokerTemplate.AddDefine("isVoid", "false");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}]");
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
                    backend = invokerTemplate.Compile();
                }

                frontend =
                    $"get => GetResultAsync<{method.ReturnType.ToUnityString()}>({index}{parametersArray}).GetAwaiter().GetResult();";
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
                            Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));

                    var valueInsert = Caster(method.Parameters.Last().Type,
                        "parameters[" + (method.Parameters.Length - 1) + "]");
                    var invokerTemplate = (TemplateDocument)_methodInvokerOriginal.Clone();


                    invokerTemplate.AddDefine("isVoid", "true");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToUnityString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToUnityString());
                    invokerTemplate.AddDefine("funcInvoking",
                        $"(i as {method.ContainingType.ToUnityString()})[{parameterInserts}] = {valueInsert}");
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
                        $"(i as {method.ContainingType.ToUnityString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")}");
                    backend = invokerTemplate.Compile();
                }

                frontend = $"set => CallAsync({index}, new System.Object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((frontend, method));
            _methodRepoTemplate.Insert("invoker", backend);
            invokers.Add(backend);
        }

        private static string ToFullString(IParameterSymbol parameter)
        {
            return parameter.Type.ToUnityString() + " " + parameter.Name;
        }

        private static string ToFullString(ITypeSymbol type)
        {
            return type.ToUnityString();
        }

        private string ExtractTaskType(ITypeSymbol taskType)
        {
            var generics = ((INamedTypeSymbol)taskType).TypeArguments;
            return generics.Length == 0 ? "void" : generics[0].ToUnityString();
        }

        private bool ContainsSoiAttribute(SyntaxList<AttributeListSyntax> attributes, GeneratorExecutionContext context,
            InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            foreach (var attributeSyntax in
                     attributes.SelectMany(attributeListSyntax => attributeListSyntax.Attributes))
            {
                if (context.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree)
                        .GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue; // if we can't get the symbol, ignore it

                var attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the [Report] attribute.
                if (attributeName == "mROA.Implementation.Attributes.SharedObjectInterfaceAttribute")
                    return true;
            }

            return false;
        }

        private List<IMethodSymbol> CollectMembers(INamedTypeSymbol type)
        {
            var methods = type.GetMembers().OfType<IMethodSymbol>().ToList();
            foreach (var inner in type.AllInterfaces) methods.AddRange(inner.GetMembers().OfType<IMethodSymbol>());

            methods.RemoveAll(m => m.Name == "Dispose");
            return methods.OrderBy(i => i.Name).ToList();
        }
    }

    public static class CodegenExtentions
    {
        public static string ToUnityString(this ITypeSymbol type)
        {
            var parts = type.ToDisplayParts();

            if (parts.Any(i => i.Kind == SymbolDisplayPartKind.Keyword))
                return parts.ToUnityString();

            return type.ToDisplayString();
        }

        public static string ToUnityString(this IParameterSymbol parameter)
        {
            return parameter.Type.ToUnityString() + " " + parameter.Name;
        }

        public static string ToUnityString(this ImmutableArray<SymbolDisplayPart> parts)
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