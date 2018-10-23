using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpecFlowLSP
{
    public class GherkinManager
    {
        private string _rootPath;
        private readonly Dictionary<string, GherkinFile> _files = new Dictionary<string, GherkinFile>();
        private readonly Parser _parser = new Parser();

        public IEnumerable<ParseErrorInformation> HandleParseRequest(in string path, in string text)
        {
            var distinctPath = Path.GetFullPath(path);
            var file = _parser.ParseFile(text, distinctPath);
            _files[distinctPath] = file;
            return file.ErrorInformation;
        }

        public void HandleStartup(in string rootPath)
        {
            _rootPath = rootPath;
            Directory.GetFiles(rootPath, "*.feature", SearchOption.AllDirectories)
                .ToList().ForEach(path => HandleParseRequest(path, File.ReadAllText(path)));
        }

        public IEnumerable<StepInfo> GetSteps()
        {
            return _files.Values.SelectMany(file => file.AllSteps).ToList();
        }

        public string GetLanguage(in string filePath)
        {
            return _files[filePath]?.Document?.Feature?.Language ?? "en";
        }
    }
}