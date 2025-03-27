// #define DONT_ADD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using mROA.CodegenTools;

namespace mROA.Codegen
{
    /// <summary>
    /// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
    /// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
    /// </summary>
    [Generator]
    public class mROASourceGenerator : ISourceGenerator
    {
        private TemplateDocument MethodRepoTemplate;
        private TemplateDocument ClassTemplateOriginal;
        private TemplateDocument ClassTemplate;
        private TemplateDocument InterfaceTemplateOriginal;
        private TemplateDocument InterfaceTemplate;
        private TemplateDocument BinderTemplate;
        private TemplateDocument MethodInvokerOriginal;
        private static Predicate<IParameterSymbol> ParameterFilter =
            i => i.Type.Name is "CancellationToken" or "RequestContext";

        private static Predicate<ITypeSymbol> ParameterFilterForType =
            i => i.Name is "CancellationToken" or "RequestContext";

        public void Initialize(GeneratorInitializationContext context)
        {
            MethodRepoTemplate = TemplateReader.FromEmbeddedResource("MethodRepo.cstmpl");
            MethodInvokerOriginal =
                ((InnerTemplateSection)MethodRepoTemplate["syncInvoker"])?.InnerTemplate;
            ClassTemplateOriginal = TemplateReader.FromEmbeddedResource("RemoteEndpoint.cstmpl");
            BinderTemplate = TemplateReader.FromEmbeddedResource("RemoteTypeBinder.cstmpl");
            InterfaceTemplateOriginal = TemplateReader.FromEmbeddedResource("PartialInterface.cstmpl");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
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
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to load method repository");
            }
        }

        private void GenerateCode(GeneratorExecutionContext context, Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> classes)
        {
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

                ClassTemplate = (TemplateDocument)ClassTemplateOriginal.Clone();
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

                            ClassTemplate.Insert("methods", impl);
                            // declaredMethods.Add(impl);
                            break;
                        case IEventSymbol eventSymbol:
                            ClassTemplate.Insert("methods",
                                $"public event {eventSymbol.Type.ToDisplayString()}? {eventSymbol.Name};");
                            // declaredMethods.Add(
                            //     $"public event {eventSymbol.Type.ToDisplayString()}? {eventSymbol.Name};");
                            break;
                    }
                }

                GenerateEventImplementation(classSymbol, invokers, context);

                ClassTemplate.AddDefine("className", className);
                ClassTemplate.AddDefine("originalName", originalName);
                ClassTemplate.AddDefine("namespaceName", namespaceName);

                var code = ClassTemplate.Compile();


                // Add the source code to the compilation.
#if !DONT_ADD
                context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                BinderTemplate.Insert("remoteTypePair",
                    $"{{ typeof({classSymbol.ToDisplayString()}), typeof({namespaceName}.{className}) }}");
            }

            if (totalMethods.Count != 0)
            {
                var methodsStringed = invokers;

   

                var coCodegenRepoCode = MethodRepoTemplate.Compile();
#if !DONT_ADD
                context.AddSource("CoCodegenMethodRepository.g.cs", SourceText.From(coCodegenRepoCode, Encoding.UTF8));
#endif
            }

            if (BinderTemplate["remoteTypePair+"] != null)
            {
                var fronendRepoCode = BinderTemplate.Compile();
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

            InterfaceTemplate = (TemplateDocument)InterfaceTemplateOriginal.Clone();
            InterfaceTemplate.AddDefine("name", classSymbol.Name);
            InterfaceTemplate.AddDefine("namespace", classSymbol.ContainingNamespace.ToDisplayString());
            var singleEventBinder = new List<string>(events.Count);
            var objectBinderTemplate =
                (TemplateDocument)((InnerTemplateSection)BinderTemplate["objectBinderTemplate"]).InnerTemplate.Clone();
            for (int i = 0; i < events.Count; i++)
            {
                var currentEvent = events[i];

                var additionalMethod = GenerateMethodExternalCaller(currentEvent, out var signature);
                ClassTemplate.Insert("methods", additionalMethod);

                // declaredMethods.Add(additionalMethod);
                InterfaceTemplate.Insert("signature", signature);
                GenerateEventCode(currentEvent, invokers, classSymbol);
                GenerateBinderCode(currentEvent, invokers, classSymbol, objectBinderTemplate);
            }

            objectBinderTemplate.AddDefine("type", classSymbol.ToDisplayString());
            var partialInterface = InterfaceTemplate.Compile();
            var binder = objectBinderTemplate.Compile();
            BinderTemplate.Insert("eventBinder", binder);

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
            interfaceSignature = signature + ";";
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
            ClassTemplate.Insert("methods", sb.ToString());

            // declaredMethods.Add(sb.ToString());
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
            {
                var invokerTemplate = (TemplateDocument)((InnerTemplateSection)MethodRepoTemplate["asyncInvoker"]).InnerTemplate.Clone();
                invokerTemplate.AddDefine("isVoid", isVoid.ToString().ToLower());
                invokerTemplate.AddDefine("returnType", ExtractTaskType(method.ReturnType));
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                backend = invokerTemplate.Compile();
            }
            else
            {
                var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
                invokerTemplate.AddDefine("isVoid",  isVoid.ToString().ToLower());
                invokerTemplate.AddDefine("returnType", isVoid ? "void" : method.ReturnType.ToDisplayString());
                invokerTemplate.AddDefine("parametersType", parameterTypes);
                invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                invokerTemplate.AddDefine("funcInvoking", funcInvoking);
                backend = invokerTemplate.Compile();
            }

            MethodRepoTemplate.Insert("invoker", backend);

            invokers.Add(backend);
        }

        private void GenerateBinderCode(IEventSymbol eventSymbol, List<string> invokers, INamedTypeSymbol baseType,
            TemplateDocument document)
        {
            var eventBinderTemplate =
                (TemplateDocument)((InnerTemplateSection)document["eventBinderTemplate"]!).InnerTemplate.Clone();

            var index = invokers.Count - 1;
            var parameters = (eventSymbol.Type as INamedTypeSymbol)!.TypeArguments.ToList();
            int parameterIndex = 0;
            var parametersDeclaration = string.Join(", ",
                JoinWithComa(Enumerable.Range(0, parameters.Count).Select(i => "p" + i++)));

            var transferParameters =
                JoinWithComa(parameters.Where(i => !ParameterFilterForType(i))
                    .Select(i => "p" + parameters.IndexOf(i)));

            string callFilter;

            var requestIndex = parameters.FindIndex(i => i.Name == "RequestContext");
            if (requestIndex != -1)
            {
                callFilter = $"\n\r\t\t\tif(ownerId == p{requestIndex}.OwnerId) return;";
                eventBinderTemplate.AddDefine("callFilter", callFilter);
            }

            eventBinderTemplate.AddDefine("type", baseType.ToDisplayString());
            eventBinderTemplate.AddDefine("eventName", eventSymbol.Name);
            eventBinderTemplate.AddDefine("parametersDeclaration", parametersDeclaration);
            eventBinderTemplate.AddDefine("commandId", index.ToString());
            eventBinderTemplate.AddDefine("transferParameters", transferParameters);
            var eventBinderCode = eventBinderTemplate.Compile();
            document.Insert("eventBinder", eventBinderCode);
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
                        parametersInsertList.Add("special[0] as RequestContext");
                        break;
                    default:
                        parametersInsertList.Add(Caster(parameter,
                            $"parameters[{parameters.IndexOf(parameter)}]"));
                        break;
                }
            }

 
            
            var parametersInsert = string.Join(", ", parametersInsertList);
            
            var funcInvoking = $@"{{
{level}        (i as {baseInterface.ToDisplayString()}).{EventExternalName(eventSymbol)}({parametersInsert});
{level}        return null;
{level}    }}";
            
            var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
            invokerTemplate.AddDefine("isVoid",  "true");
            invokerTemplate.AddDefine("returnType", "void");
            invokerTemplate.AddDefine("parametersType", parameterTypes);
            invokerTemplate.AddDefine("suitableType", baseInterface.ToDisplayString());
            invokerTemplate.AddDefine("funcInvoking", funcInvoking);
            var backend = invokerTemplate.Compile();
            MethodRepoTemplate.Insert("invoker", backend);

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
                    
                    var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
                    invokerTemplate.AddDefine("isVoid",  "false");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToDisplayString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                    invokerTemplate.AddDefine("funcInvoking", $"(i as {method.ContainingType.ToDisplayString()})[{parameterInserts}]");
                    backend = invokerTemplate.Compile();
//                     backend = $@"new mROA.Implementation.MethodInvoker 
// {level}{{
// {level}    IsVoid = false,
// {level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
// {level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
// {level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
// {level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()})[{parameterInserts}],
// {level}}}";
                }
                else
                {
                    var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
                    invokerTemplate.AddDefine("isVoid",  "false");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToDisplayString());
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                    invokerTemplate.AddDefine("funcInvoking", $"(i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name}");
                    backend = invokerTemplate.Compile();
//                     backend = $@"new mROA.Implementation.MethodInvoker 
// {level}{{
// {level}    IsVoid = false,
// {level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
// {level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
// {level}    Invoking = (i, _, _) => (i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name},
// {level}}}";
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
                            Caster(p.Type, "parameters[" + method.Parameters.IndexOf(p) + "]")));

                    var valueInsert = Caster(method.Parameters.Last().Type,
                        "parameters[" + (method.Parameters.Length - 1) + "]");
                    var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
                    
                    
                    invokerTemplate.AddDefine("isVoid",  "true");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToDisplayString());
                    invokerTemplate.AddDefine("parametersType", parameterTypes);
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                    invokerTemplate.AddDefine("funcInvoking", $"(i as {method.ContainingType.ToDisplayString()})[{parameterInserts}] = {valueInsert}");
                    backend = invokerTemplate.Compile();
//                     backend = $@"new mROA.Implementation.MethodInvoker 
// {level}{{
// {level}    IsVoid = true,
// {level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
// {level}    ParameterTypes = new Type[] {{ {parameterTypes} }},
// {level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
// {level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()})[{parameterInserts}] = {valueInsert},
// {level}}}";
                }
                else
                {
                    var invokerTemplate = (TemplateDocument)MethodInvokerOriginal.Clone();
                    
                    invokerTemplate.AddDefine("isVoid",  "true");
                    invokerTemplate.AddDefine("returnType", method.ReturnType.ToDisplayString());
                    invokerTemplate.AddDefine("parametersType", $"typeof({method.Parameters.First().Type.ToDisplayString()})");
                    invokerTemplate.AddDefine("suitableType", baseInterace.ToDisplayString());
                    invokerTemplate.AddDefine("funcInvoking", $"(i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")}");
                    backend = invokerTemplate.Compile();
                    
//                     backend = $@"new mROA.Implementation.MethodInvoker 
// {level}{{
// {level}    IsVoid = true,
// {level}    ReturnType = typeof({method.ReturnType.ToDisplayString()}),
// {level}    ParameterTypes = new Type[] {{ typeof({method.Parameters.First().Type.ToDisplayString()}) }},
// {level}    SuitableType = typeof({baseInterace.ToDisplayString()}),
// {level}    Invoking = (i, parameters, _) => (i as {method.ContainingType.ToDisplayString()}).{(method.AssociatedSymbol as IPropertySymbol)!.Name} = {Caster((method.AssociatedSymbol as IPropertySymbol)!.Type, "parameters[0]")},
// {level}}}";
                }

                frontend = $"set => CallAsync({index}, new object[] {{ {parametersArray} }}).Wait();";
            }

            propsCollection.Add((frontend, method));
            MethodRepoTemplate.Insert("invoker", backend);
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
            var generics = ((INamedTypeSymbol)taskType).TypeArguments;
            return generics.Length == 0 ? "void" : generics[0].ToDisplayString();
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