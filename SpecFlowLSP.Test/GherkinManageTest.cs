using System.Collections.Generic;
using Gherkin.Ast;
using Xunit;

namespace SpecFlowLSP.Test
{
    public class GherkinManageTest
    {
        [Theory]
        [InlineData("#language:de","de")]
        [InlineData("#language: de","de")]
        [InlineData("#language : de","de")]
        [InlineData("#  language   :  de","de")]
        [InlineData("#lan guage:de","en")]
        public void TryLanguage_CorrectReturned(in string testString, in string expected)
        {
            var actual = new GherkinManager().TryLanguage(testString);
            Assert.Equal(expected,actual);
        }
    }
}