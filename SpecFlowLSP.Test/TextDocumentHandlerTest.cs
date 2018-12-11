using Gherkin;
using Gherkin.Ast;
using Xunit;

namespace SpecFlowLSP.Test
{
    public class TextDocumentHandlerTest
    {
        [Theory]
        [InlineData("Given I am", "en", "I am")]
        [InlineData("Given     I am", "en", "I am")]
        [InlineData("Gegeben sei I am", "de", "I am")]
        public void GetStep_CorrectStep(in string fullStep, in string language, in string expected)
        {
            var actual = GherkinDocumentHandler.GetStep(fullStep, new GherkinDialectProvider().GetDialect(language, new Location()));
            Assert.Equal(expected, actual);
        }
    }
}