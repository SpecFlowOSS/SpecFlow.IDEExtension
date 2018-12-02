using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Ast;

namespace SpecFlowLSP
{
    public class GherkinManager
    {
        private static readonly GherkinDialectProvider DialectProvider = new GherkinDialectProvider();
        private string _rootPath;
        private readonly Dictionary<string, GherkinFile> _parsedFiles = new Dictionary<string, GherkinFile>();
        private readonly Dictionary<string, string> _openFiles = new Dictionary<string, string>();

        public IEnumerable<ParseErrorInformation> HandleFileRequest(in string path, in string text)
        {
            var distinctPath = Path.GetFullPath(path);
            _openFiles[distinctPath] = text;
            return HandleParseRequest(distinctPath, text);
        }

        public void HandleCloseRequest(in string path)
        {
            var distinctPath = Path.GetFullPath(path);
            _openFiles.Remove(distinctPath);
        }

        private IEnumerable<ParseErrorInformation> HandleParseRequest(in string distinctPath, in string text)
        {
            var file = Parser.ParseFile(text, distinctPath);
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
                .ToList().ForEach(path => HandleParseRequest(path, File.ReadAllText(path)));
        }

        public IEnumerable<StepInfo> GetSteps()
        {
            return _parsedFiles.Values.SelectMany(file => file.AllSteps).ToList();
        }

        public GherkinDialect GetLanguage(in string filePath)
        {
            return GetDialect(ParseLanguage(_openFiles[filePath]));
        }
        
        private static GherkinDialect GetDialect(string featureLanguage)
        {
            GherkinDialect dialect;
            try
            {
                dialect = DialectProvider.GetDialect(featureLanguage, new Location());
            }
            catch (NoSuchLanguageException)
            {
                dialect = DialectProvider.DefaultDialect;
            }

            return dialect;
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
    }
}