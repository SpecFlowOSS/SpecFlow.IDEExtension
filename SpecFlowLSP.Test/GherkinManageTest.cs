using Xunit;

namespace SpecFlowLSP.Test
{
    public class GherkinManageTest
    {
        [Theory]
        [InlineData("#language:de", "de")]
        [InlineData("#language: de", "de")]
        [InlineData("#language : de", "de")]
        [InlineData("#language : de\r\n", "de")]
        [InlineData("#language : de   \r\n", "de")]
        [InlineData("#language : de\n", "de")]
        [InlineData("#language : de   \n", "de")]
        [InlineData("#  language   :  de", "de")]
        [InlineData("#lan guage:de", "en")]
        public void ParseLanguage_CorrectReturned(in string testString, in string expected)
        {
            var actual = new GherkinManager().ParseLanguage(testString);
            Assert.Equal(expected, actual);
        }
    }
}