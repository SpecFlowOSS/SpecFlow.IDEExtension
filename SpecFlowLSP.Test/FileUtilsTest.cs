using System.Collections.Generic;
using Xunit;

namespace SpecFlowLSP.Test
{
    public class FileUtilsTest
    {
        [Fact]
        public void SplitString_NewLine_StringSplit()
        {
            const string testString = "Hello\nWorld";
            var actual = FileUtils.SplitString(testString);

            var expected = new List<string> {"Hello", "World"};

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SplitString_CarriageReturn_StringSplit()
        {
            const string testString = "Hello\rWorld";
            var actual = FileUtils.SplitString(testString);

            var expected = new List<string> {"Hello", "World"};

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SplitString_NewLineAndCarriageReturn_StringSplit()
        {
            const string testString = "Hello\r\nWorld";
            var actual = FileUtils.SplitString(testString);

            var expected = new List<string> {"Hello", "World"};

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SplitString_NewLineWithSpaces_StringSplit()
        {
            const string testString = "Hello World\nWhat are you doing today?";
            var actual = FileUtils.SplitString(testString);

            var expected = new List<string> {"Hello World", "What are you doing today?"};

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SplitString_MultipleEmptyLines_StringSplit()
        {
            const string testString = "Hello World\n\n\nWhat are you doing today?";
            var actual = FileUtils.SplitString(testString);

            var expected = new List<string> {"Hello World", "", "", "What are you doing today?"};

            Assert.Equal(expected, actual);
        }
    }
}