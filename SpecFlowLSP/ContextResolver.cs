using System.Collections.Generic;
using System.Linq;

namespace SpecFlowLSP
{
    public static class ContextResolver
    {
        public static CompletionContext ResolveContext(in IList<string> text, in GherkinFile parsedFile,
            in int line)
        {
            return ResolveContext(text, line, parsedFile?.Document?.Feature?.Language ?? "en");
        }

        public static CompletionContext ResolveContext(in IList<string> text,
            in int lineIndex, in string featureLanguage)
        {
            var allKeywords = KeywordProvider.GetAllKeywordsForLanguage(featureLanguage);
            if (text.Count > lineIndex)
            {
                var currentLineTrimmed = text[lineIndex].Trim();

                if (IsStepContext(currentLineTrimmed, allKeywords))
                {
                    return CompletionContext.Step;
                }

                if (AlreadyBeginsWithNotStepKeyword(currentLineTrimmed, allKeywords))
                {
                    return CompletionContext.None;
                }
            }

            return SearchForContext(text, lineIndex, allKeywords);
        }

        private static CompletionContext SearchForContext(in IList<string> text, in int lineIndex,
            in IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords)
        {
            for (var i = lineIndex - 1; i >= 0; i--)
            {
                var line = text[i].Trim();
                var scenarioOutlineKeywords =
                    AllKeywordsOfTypes(allKeywords, new[] {KeywordProvider.GherkinKeyword.ScenarioOutline});
                var scenarioKeywords = AllKeywordsOfTypes(allKeywords, new[] {KeywordProvider.GherkinKeyword.Scenario});
                var featureKeyword = AllKeywordsOfTypes(allKeywords, new[] {KeywordProvider.GherkinKeyword.Feature});
                if (LineStartsWith(line, scenarioOutlineKeywords))
                {
                    return CompletionContext.ScenarioOutline;
                }

                if (LineStartsWith(line, scenarioKeywords))
                {
                    return CompletionContext.Scenario;
                }

                if (LineStartsWith(line, featureKeyword))
                {
                    return CompletionContext.Feature;
                }
            }

            return CompletionContext.Root;
        }


        private static bool AlreadyBeginsWithNotStepKeyword(string trimmedLine,
            in IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords)
        {
            var allNonStepKeywords = AllNonStepKeywords(allKeywords);
            return LineStartsWith(trimmedLine, allNonStepKeywords);
        }

        private static bool IsStepContext(string trimmedLine,
            in IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords)
        {
            var allStepKeywords = AllStepKeywords(allKeywords);
            return LineStartsWith(trimmedLine, allStepKeywords);
        }

        private static bool LineStartsWith(string trimmedLine, IEnumerable<string> allStepKeywords)
        {
            return allStepKeywords.Any(trimmedLine.StartsWith);
        }

        private static IEnumerable<string> AllNonStepKeywords(
            in IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords)
        {
            return AllKeywordsOfTypes(allKeywords, new[]
            {
                KeywordProvider.GherkinKeyword.Feature,
                KeywordProvider.GherkinKeyword.Background,
                KeywordProvider.GherkinKeyword.Scenario,
                KeywordProvider.GherkinKeyword.ScenarioOutline,
                KeywordProvider.GherkinKeyword.Examples
            });
        }

        private static IEnumerable<string> AllStepKeywords(
            IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords)
        {
            return AllKeywordsOfTypes(allKeywords, new[]
            {
                KeywordProvider.GherkinKeyword.Given,
                KeywordProvider.GherkinKeyword.When,
                KeywordProvider.GherkinKeyword.Then,
                KeywordProvider.GherkinKeyword.And,
                KeywordProvider.GherkinKeyword.But
            });
        }

        private static IEnumerable<string> AllKeywordsOfTypes(
            IDictionary<KeywordProvider.GherkinKeyword, KeywordProvider.LocalizedKeywords> allKeywords,
            IEnumerable<KeywordProvider.GherkinKeyword> types)
        {
            return allKeywords.Where(keyword => types.Contains(keyword.Key))
                .SelectMany(keyword => keyword.Value.AllKeywords);
        }

        public static IEnumerable<string> GetAllKeywordsForContext(in CompletionContext context,
            in string language)
        {
            var allKeywords = KeywordProvider.GetAllKeywordsForLanguage(language);
            var gherkinKeywords = ContextKeywordMapping[context];
            return AllKeywordsOfTypes(allKeywords, gherkinKeywords);
        }


        private static readonly Dictionary<CompletionContext, IList<KeywordProvider.GherkinKeyword>>
            ContextKeywordMapping =
                new Dictionary<CompletionContext, IList<KeywordProvider.GherkinKeyword>>
                {
                    {
                        CompletionContext.Root,
                        new List<KeywordProvider.GherkinKeyword> {KeywordProvider.GherkinKeyword.Feature}
                    },
                    {
                        CompletionContext.Feature,
                        new List<KeywordProvider.GherkinKeyword>
                        {
                            KeywordProvider.GherkinKeyword.Scenario, KeywordProvider.GherkinKeyword.ScenarioOutline,
                            KeywordProvider.GherkinKeyword.Background
                        }
                    },
                    {
                        CompletionContext.Scenario,
                        new List<KeywordProvider.GherkinKeyword>
                        {
                            KeywordProvider.GherkinKeyword.Scenario, KeywordProvider.GherkinKeyword.ScenarioOutline,
                            KeywordProvider.GherkinKeyword.Background, KeywordProvider.GherkinKeyword.Given,
                            KeywordProvider.GherkinKeyword.When,
                            KeywordProvider.GherkinKeyword.Then, KeywordProvider.GherkinKeyword.And,
                            KeywordProvider.GherkinKeyword.But
                        }
                    },
                    {
                        CompletionContext.ScenarioOutline,
                        new List<KeywordProvider.GherkinKeyword>
                        {
                            KeywordProvider.GherkinKeyword.Scenario, KeywordProvider.GherkinKeyword.ScenarioOutline,
                            KeywordProvider.GherkinKeyword.Background, KeywordProvider.GherkinKeyword.Given,
                            KeywordProvider.GherkinKeyword.When,
                            KeywordProvider.GherkinKeyword.Then, KeywordProvider.GherkinKeyword.And,
                            KeywordProvider.GherkinKeyword.But, KeywordProvider.GherkinKeyword.Examples
                        }
                    },
                    {
                        CompletionContext.Step,
                        new List<KeywordProvider.GherkinKeyword>()
                    },
                    {
                        CompletionContext.None,
                        new List<KeywordProvider.GherkinKeyword>()
                    }
                };
    }

    public enum CompletionContext
    {
        Root,
        Feature,
        Scenario,
        ScenarioOutline,
        Step,
        None
    }
}