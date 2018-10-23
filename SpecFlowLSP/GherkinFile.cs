using System;
using System.Collections.Generic;
using System.Linq;
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
            FilePath = path;
            Document = document;
            ErrorInformation = errorInformation;
            _allStepsLazy = new Lazy<IEnumerable<StepInfo>>(CalculateSteps);
        }



        private readonly Lazy<IEnumerable<StepInfo>> _allStepsLazy;
        public IEnumerable<StepInfo> AllSteps => _allStepsLazy.Value;
        public string FilePath { get; }
        public bool HasError => ErrorInformation.Any();
        public GherkinDocument Document { get; }
        public IEnumerable<ParseErrorInformation> ErrorInformation { get; }

        private IEnumerable<StepInfo> CalculateSteps()
        {
            return Document?.Feature?.Children?.SelectMany(scenario => scenario.Steps).Select(ToStepInfo).ToList()
                   ?? Enumerable.Empty<StepInfo>();
        }

        private StepInfo ToStepInfo(Step step)
        {
            return new StepInfo(step.Text, FilePath, step.Location.Line);
        }
    }
}