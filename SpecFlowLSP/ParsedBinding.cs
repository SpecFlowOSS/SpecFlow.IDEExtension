using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SpecFlowLSP
{
    public class ParsedBinding
    {
        public string Path { get; set; }
        public IEnumerable<StepInfo> Steps { get; set; }
        public SyntaxTree SyntaxTree { get; set; }
    }
}