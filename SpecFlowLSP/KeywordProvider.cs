using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SpecFlowLSP
{
    public static class KeywordProvider
    {
        private static readonly IDictionary<string, KeywordDictionary> Keywords = ParseKeywords();

        private static IDictionary<string, KeywordDictionary> ParseKeywords()
        {
            var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var localizationPath = Path.Combine(workingDir, "localization");
            return Directory.EnumerateFiles(localizationPath).Select(ParseFile)
                .ToDictionary(entry => entry.LanguageIdentifier);
        }

        private static KeywordDictionary ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var dict = lines.Select(ParseLine).ToDictionary(keyword => keyword.Identifier);

            var languageId = Path.GetFileName(filePath).Split('.')[0];
            return new KeywordDictionary {LanguageIdentifier = languageId, KeywordMapping = dict};
        }

        private static LocalizedKeywords ParseLine(string line)
        {
            var idKeyword = line.Split(';');
            var keywords = idKeyword[1].Split(',');
            return new LocalizedKeywords(idKeyword[0], keywords);
        }

        public static IDictionary<GherkinKeyword, LocalizedKeywords> GetAllKeywordsForLanguage(in string languageId)
        {
            return Keywords[languageId].KeywordMapping;
        }

        private class KeywordDictionary
        {
            public string LanguageIdentifier;
            public Dictionary<GherkinKeyword, LocalizedKeywords> KeywordMapping;
        }

        public class LocalizedKeywords
        {
            public readonly GherkinKeyword Identifier;
            public readonly string[] AllKeywords;

            internal LocalizedKeywords(string keywordString, string[] localizedKeywords)
            {
                Identifier = GetIdentifier(keywordString);
                AllKeywords = localizedKeywords;
            }

            private static GherkinKeyword GetIdentifier(string keywordString)
            {
                switch (keywordString)
                {
                    case "feature": return GherkinKeyword.Feature;
                    case "background": return GherkinKeyword.Background;
                    case "scenario": return GherkinKeyword.Scenario;
                    case "scenarioOutline": return GherkinKeyword.ScenarioOutline;
                    case "examples": return GherkinKeyword.Examples;
                    case "given": return GherkinKeyword.Given;
                    case "when": return GherkinKeyword.When;
                    case "then": return GherkinKeyword.Then;
                    case "and": return GherkinKeyword.And;
                    case "but": return GherkinKeyword.But;
                    default: throw new InvalidDataException($"{keywordString} is not a valid gherkin keyword");
                }
            }
        }

        public enum GherkinKeyword
        {
            Feature,
            Background,
            Scenario,
            ScenarioOutline,
            Examples,
            Given,
            When,
            Then,
            And,
            But
        }
    }
}