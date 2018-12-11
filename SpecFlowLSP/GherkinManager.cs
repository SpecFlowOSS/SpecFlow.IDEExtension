using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using Microsoft.CodeAnalysis;
using Location = Gherkin.Ast.Location;

namespace SpecFlowLSP
{
    public class GherkinManager
    {
        private static readonly GherkinDialectProvider DialectProvider = new GherkinDialectProvider();
        private string _rootPath;
        private readonly Dictionary<string, GherkinFile> _parsedFiles = new Dictionary<string, GherkinFile>();
        private readonly Dictionary<string, CsharpBinding> _csharpBindings = new Dictionary<string, CsharpBinding>();
        private readonly Dictionary<string, string> _openFiles = new Dictionary<string, string>();
        private Compilation _compilation;

        public IEnumerable<ParseErrorInformation> HandleFileRequest(in string path, in string text)
        {
            var distinctPath = Path.GetFullPath(path);
            _openFiles[distinctPath] = text;
            return HandleFeatureParseRequest(distinctPath, text);
        }

        public void HandleCloseRequest(in string path)
        {
            var distinctPath = Path.GetFullPath(path);
            _openFiles.Remove(distinctPath);
        }

        private IEnumerable<ParseErrorInformation> HandleFeatureParseRequest(in string distinctPath, in string text)
        {
            var file = GherkinParser.ParseFile(text, distinctPath);
            if (!file.HasError)
            {
                _parsedFiles[distinctPath] = file;
            }

            return file.ErrorInformation;
        }

        public void HandleStartup(in string rootPath)
        {
            _rootPath = rootPath;
            Directory.GetFiles(rootPath, "*.feature", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .ToList().ForEach(path => HandleFeatureParseRequest(path, File.ReadAllText(path)));
            Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .ToList().ForEach(HandleCsharpParseRequest);
            CreateInitialCsharpCompilation();
            FetchAllSteps();
        }

        private void HandleCsharpParseRequest(string path)
        {
            var syntaxTree = CsharpParser.GetSyntaxTreeIfItContainsBindingsFromFile(path);
            if (syntaxTree != null)
            {
                _csharpBindings[path] = new CsharpBinding {Path = path, SyntaxTree = syntaxTree};
            }
        }

        private void CreateInitialCsharpCompilation()
        {
            _compilation =
                CsharpParser.GetCompilationFromSyntaxTrees(
                    _csharpBindings.Values.Select(binding => binding.SyntaxTree));
        }

        private void FetchAllSteps()
        {
            _csharpBindings.Values.ToList().ForEach(CalculateStepsFromBinding);
        }

        private void CalculateStepsFromBinding(CsharpBinding binding)
        {
            var steps = CsharpParser.GetBindings(_compilation, binding.SyntaxTree);
            var stepInfos = steps.Select(step => new StepInfo(step, binding.Path, 0));
            binding.Steps = stepInfos;
        }

        public IEnumerable<StepInfo> GetSteps()
        {
            return _parsedFiles.Values.SelectMany(file => file.AllSteps)
                .Union(
                    _csharpBindings.Values.SelectMany(binding => binding.Steps))
                .ToList();
        }

        public GherkinDialect GetLanguage(in string filePath)
        {
            return GetDialect(ParseLanguage(_openFiles[filePath]));
        }

        private static GherkinDialect GetDialect(string featureLanguage)
        {
            try
            {
                return DialectProvider.GetDialect(featureLanguage, new Location());
            }
            catch (NoSuchLanguageException)
            {
                return DialectProvider.DefaultDialect;
            }
        }

        public string ParseLanguage(in string text)
        {
            var language = Regex.Match(text, @"#\s*language\s*:\s*(.*)");
            return language.Success ? language.Groups[1].Value : GetDefaultLanguage();
        }

        private string GetDefaultLanguage()
        {
            return "en";
        }

        public IList<string> GetFile(string filePath)
        {
            var file = _openFiles[filePath];
            return FileUtils.SplitString(file);
        }

        public void HandleCsharpFileChanged(in string path, in string text)
        {
            var fullPath = Path.GetFullPath(path);

            var newSyntaxTree = CsharpParser.GetSyntaxTreeIfItContainsBindingsFromText(text);
            CsharpBinding binding;
            
            if (newSyntaxTree == null) return;
            
            if (_csharpBindings.ContainsKey(fullPath))
            {
                binding = _csharpBindings[fullPath];
                _compilation = CsharpParser.ReplaceInCompilation(_compilation, binding.SyntaxTree, newSyntaxTree);
                binding.SyntaxTree = newSyntaxTree;
            }
            else
            {
                _compilation = CsharpParser.AddToCompilation(_compilation, newSyntaxTree);
                binding = new CsharpBinding {Path = path, SyntaxTree = newSyntaxTree};
                _csharpBindings[fullPath] = binding;
            }

            CalculateStepsFromBinding(binding);
        }
    }
}