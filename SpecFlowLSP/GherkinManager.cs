using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;
using Gherkin.Ast;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace SpecFlowLSP
{
    public class GherkinManager
    {
        private string _rootPath;
        private readonly Dictionary<string, GherkinFile> _files = new Dictionary<string, GherkinFile>();
        private readonly Parser _parser = new Parser();

        public IEnumerable<ParseErrorInformation> HandleParseRequest(in string path, in string text)
        {
            var file = _parser.ParseFile(text, path);
            _files[path] = file;
            return file.ErrorInformation;
        }

        public void HandleStartup(in string rootPath)
        {
            _rootPath = rootPath;
            Directory.GetFiles(rootPath, "*.feature", SearchOption.AllDirectories)
                .ToList().ForEach(path => HandleParseRequest(path, File.ReadAllText(path)));
        }

        public IEnumerable<Step> GetSteps()
        {
            return _files.Values.SelectMany(file => file.AllSteps).ToList();
        }
    }
}