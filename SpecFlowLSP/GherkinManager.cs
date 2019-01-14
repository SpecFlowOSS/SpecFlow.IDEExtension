using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Events.Args.Pickle;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Location = Gherkin.Ast.Location;

namespace SpecFlowLSP
{
    public class GherkinManager
    {
        private static readonly GherkinDialectProvider DialectProvider = new GherkinDialectProvider();
        private string _rootPath;
        private readonly Dictionary<string, GherkinFile> _parsedFeatureFiles = new Dictionary<string, GherkinFile>();
        private readonly Dictionary<string, ParsedBinding> _csharpBindings = new Dictionary<string, ParsedBinding>();
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
                _parsedFeatureFiles[distinctPath] = file;
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
            SetUpFileWatcher(rootPath);
        }

        private void SetUpFileWatcher(in string rootPath)
        {
            var watcher = new FileSystemWatcher
            {
                Path = rootPath,
                Filter = "*.cs",
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.DirectoryName |
                               NotifyFilters.FileName,
                IncludeSubdirectories = true
            };

            watcher.Created += OnCsharpCreated;
            watcher.Changed += OnCsharpChanged;
            watcher.Deleted += OnCsharpDeleted;
            watcher.Renamed += OnCsharpRenamed;

            watcher.EnableRaisingEvents = true;
        }

        private void OnCsharpCreated(object sender, FileSystemEventArgs e)
        {
            HandleCsharpFileChanged(e.FullPath);
        }

        private void OnCsharpChanged(object sender, FileSystemEventArgs e)
        {
            HandleCsharpFileChanged(e.FullPath);
        }

        private void OnCsharpDeleted(object sender, FileSystemEventArgs e)
        {
            _csharpBindings.Remove(e.FullPath);
        }

        private void OnCsharpRenamed(object sender, RenamedEventArgs e)
        {
            if (e.FullPath.EndsWith(".cs"))
            {
                if (_csharpBindings.ContainsKey(e.OldFullPath))
                {
                    _csharpBindings[e.FullPath] = _csharpBindings[e.OldFullPath];
                    _csharpBindings.Remove(e.OldFullPath);
                }
                else
                {
                    HandleCsharpFileChanged(e.FullPath);
                }
            }
        }

        private void HandleCsharpParseRequest(string path)
        {
            var syntaxTree = CsharpParser.GetSyntaxTreeIfItContainsBindingsFromFile(path);
            if (syntaxTree != null)
            {
                _csharpBindings[path] = new ParsedBinding {Path = path, SyntaxTree = syntaxTree};
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

        private void CalculateStepsFromBinding(ParsedBinding parsedBinding)
        {
            var steps = CsharpParser.GetBindings(_compilation, parsedBinding.SyntaxTree);
            var stepInfos = steps.Select(bindingResult =>
                new StepInfo(bindingResult.Step, parsedBinding.Path, bindingResult.Position));
            parsedBinding.Steps = stepInfos;
        }

        public IEnumerable<StepInfo> GetSteps()
        {
            return _parsedFeatureFiles.Values.SelectMany(file => file.AllSteps)
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
                return DialectProvider.GetDialect(featureLanguage, new Gherkin.Ast.Location());
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

        public void HandleCsharpFileChanged(in string path)
        {
            var fullPath = Path.GetFullPath(path);

            var newSyntaxTree = CsharpParser.GetSyntaxTreeIfItContainsBindingsFromFile(path);
            ParsedBinding binding;

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
                binding = new ParsedBinding {Path = path, SyntaxTree = newSyntaxTree};
                _csharpBindings[fullPath] = binding;
            }

            CalculateStepsFromBinding(binding);
        }

        public IEnumerable<Location> GetLocations(Position position, string path)
        {
            try
            {
                var step = FindStep(position, Path.GetFullPath(path));

                return _csharpBindings.Values
                    .SelectMany(binding => binding.Steps)
                    .Where(bindingStep => Regex.IsMatch(step.Text, bindingStep.Text))
                    .Select(ToLocation);
            }
            catch (InvalidOperationException)
            {
                return Enumerable.Empty<Location>();
            }
        }

        private static Location ToLocation(StepInfo step)
        {
           return new Location(step.Position, step.FilePath);
        }

        private StepInfo FindStep(Position position, string path)
        {
            if (_parsedFeatureFiles.ContainsKey(path))
            {
                return _parsedFeatureFiles[path].AllSteps
                    .First(step => step.Position.Start.Line == position.Line);
            }

            throw new InvalidOperationException("Step not found!");
        }
    }
}