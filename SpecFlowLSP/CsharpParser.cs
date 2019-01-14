using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TechTalk.SpecFlow;

namespace SpecFlowLSP
{
    public static class CsharpParser
    {
        public static SyntaxTree GetSyntaxTreeIfItContainsBindingsFromFile(in string path)
        {
            var text = File.ReadAllText(path);
            return GetSyntaxTreeIfItContainsBindingsFromText(text);
        }

        public static SyntaxTree GetSyntaxTreeIfItContainsBindingsFromText(in string text)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var containsBindings = syntaxTree.GetCompilationUnitRoot(CancellationToken.None).DescendantNodes()
                .OfType<AttributeSyntax>()
                .Any(attribute => attribute.Name.ToString().Equals("Binding"));
            return containsBindings ? syntaxTree : null;
        }

        public static Compilation GetCompilationFromSyntaxTrees(in IEnumerable<SyntaxTree> trees)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var netStandard = assembly.GetManifestResourceStream("SpecFlowLSP.Assemblies.netstandard.dll");

            return CSharpCompilation.Create("HelloWorld")
                .AddReferences(
                    MetadataReference.CreateFromFile(
                        typeof(BindingAttribute).Assembly.Location),
                    MetadataReference.CreateFromStream(netStandard))
                .AddSyntaxTrees(trees)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public static IEnumerable<BindingResult> GetBindings(in Compilation compilation, in SyntaxTree syntaxTree)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            return syntaxTree.GetCompilationUnitRoot(CancellationToken.None).DescendantNodes()
                .OfType<AttributeSyntax>()
                .Where(attribute => attribute.Name.ToString().Equals("Binding"))
                .Select(attribute => attribute.Parent?.Parent)
                .OfType<ClassDeclarationSyntax>()
                .SelectMany(classDecl => classDecl.DescendantNodes())
                .OfType<MethodDeclarationSyntax>()
                .Select(method => semanticModel.GetDeclaredSymbol(method, CancellationToken.None))
                .SelectMany(methodSymbol => methodSymbol.GetAttributes().Select(attribute =>
                    new AttributeInfo {Attribute = attribute, Method = methodSymbol}))
                .Where(attributeInfo =>
                    attributeInfo.Attribute.AttributeClass.BaseType.Name == "StepDefinitionBaseAttribute")
                .Select(GetStepFromAttributeInfo)
                .ToList();
        }

        private static BindingResult GetStepFromAttributeInfo(AttributeInfo attributeInfo)
        {
            var attribute = attributeInfo.Attribute;
            var step = GetStep(attributeInfo, attribute);

            var attributeRange = GetAttributeRange(attribute);

            return new BindingResult(step, attributeRange);
        }

        private static string GetStep(AttributeInfo attributeInfo, AttributeData attribute)
        {
            var ctorArguments = attribute.ConstructorArguments;
            var namedArguments = attribute.NamedArguments;

            if (ctorArguments.Length > 0)
            {
                return ctorArguments[0].Value.ToString();
            }

            return namedArguments.Any(argument => argument.Key == "Regex")
                ? namedArguments.Single(argument => argument.Key == "Regex").Value.Value.ToString()
                : attributeInfo.Method.Name;
        }

        private static Range GetAttributeRange(AttributeData attribute)
        {
            var attributeSpan = attribute.ApplicationSyntaxReference.Span;
            var lines = attribute.ApplicationSyntaxReference.SyntaxTree.GetText(CancellationToken.None).Lines;
            var attributePos = lines.GetLinePositionSpan(attributeSpan);
            var attributeRange = new Range(new Position(attributePos.Start.Line, attributePos.Start.Character),
                new Position(attributePos.End.Line, attributePos.End.Character));
            return attributeRange;
        }

        public struct BindingResult
        {
            public BindingResult(in string step, in Range position)
            {
                Step = step;
                Position = position;
            }

            public string Step { get; }
            public Range Position { get; }
        }

        private class AttributeInfo
        {
            public IMethodSymbol Method { get; set; }
            public AttributeData Attribute { get; set; }
        }

        public static Compilation ReplaceInCompilation(Compilation compilation, in SyntaxTree oldSyntaxTree,
            in SyntaxTree newSyntaxTree)
        {
            return compilation.ReplaceSyntaxTree(oldSyntaxTree, newSyntaxTree);
        }

        public static Compilation AddToCompilation(in Compilation compilation, in SyntaxTree newSyntaxTree)
        {
            return compilation.AddSyntaxTrees(newSyntaxTree);
        }
    }
}