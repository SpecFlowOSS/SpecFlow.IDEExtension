using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;

namespace SpecFlowLSP
{
    public static class GherkinParser
    {
        private static readonly Parser GherkinParserInstance = new Parser();

        public static GherkinFile ParseFile(in string text, in string path)
        {
            try
            {
                using (var stringReader = new StringReader(text))
                {
                    var gherkinDocument = GherkinParserInstance.Parse(stringReader);
                    return new GherkinFile(path, Enumerable.Empty<ParseErrorInformation>(),
                        gherkinDocument);
                }
            }
            catch (CompositeParserException cpe)
            {
                return new GherkinFile(path, ToErrorInformation(cpe.Errors, text));
            }
            catch (ParserException pe)
            {
                return new GherkinFile(path, new List<ParseErrorInformation> {ToErrorInformation(pe, text)});
            }
        }

        private static IEnumerable<ParseErrorInformation> ToErrorInformation(
            in IEnumerable<ParserException> errors, string text)
        {
            var splitFile = FileUtils.SplitString(text);
            return errors.Select(error => ToErrorInformation(error, splitFile));
        }

        private static ParseErrorInformation ToErrorInformation(in ParserException error, in string text)
        {
            return ToErrorInformation(error, FileUtils.SplitString(text));
        }

        private static ParseErrorInformation ToErrorInformation(in ParserException error, in List<string> splitFile)
        {
            return new ParseErrorInformation(error.Message, GetErrorLocationFromText(error, splitFile));
        }

        private static ErrorRange GetErrorLocationFromText(in ParserException error,
            in List<string> splitFile)
        {
            var lineIdx = Math.Clamp(error.Location.Line - 1, 0, splitFile.Count - 1);
            var line = splitFile[lineIdx];
            var startCharIndex = Math.Clamp(error.Location.Column - 1, 0 , line.Length);
            var endCharIdx = line.IndexOf(' ', startCharIndex);
            if (endCharIdx < 0)
            {
                endCharIdx = line.Length;
            }

            return new ErrorRange(new ErrorLocation(lineIdx, startCharIndex), new ErrorLocation(lineIdx, endCharIdx));
        }
    }
}