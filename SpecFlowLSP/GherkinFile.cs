using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin;
using Gherkin.Ast;

namespace SpecFlowLSP
{
    public class GherkinFile
    {
        public GherkinFile(in string path, in IEnumerable<ParseErrorInformation> errorInformation) :
            this(path, errorInformation, null)
        {
        }


        public GherkinFile(in string path, in IEnumerable<ParseErrorInformation> errorInformation,
            in GherkinDocument document)
        {
            Filepath = path;
            Document = document;
            ErrorInformation = errorInformation;
            _allStepsLazy = new Lazy<IEnumerable<Step>>(CalculateSteps);
        }



        private readonly Lazy<IEnumerable<Step>> _allStepsLazy;
        public IEnumerable<Step> AllSteps => _allStepsLazy.Value;
        public string Filepath { get; }
        public bool HasError => ErrorInformation.Any();
        public GherkinDocument Document { get; }
        public IEnumerable<ParseErrorInformation> ErrorInformation { get; }

        private IEnumerable<Step> CalculateSteps()
        {
            return Document?.Feature?.Children?.SelectMany(scenario => scenario.Steps) ?? Enumerable.Empty<Step>();
        }
    }
}