using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin;
using Gherkin.Ast;

namespace SpecFlowLSP
{
    public static class ContextResolver
    {
        private static readonly GherkinDialectProvider DialectProvider = new GherkinDialectProvider();

        public static CompletionContext ResolveContext(in IList<string> text,
            in int lineIndex, in string featureLanguage)
        {
            var dialect = GetDialect(featureLanguage);
            
            if (text.Count > lineIndex)
            {
                var currentLineTrimmed = text[lineIndex].Trim();

                if (IsStepContext(currentLineTrimmed, dialect))
                {
                    return CompletionContext.Step;
                }

                if (AlreadyBeginsWithNotStepKeyword(currentLineTrimmed, dialect))
                {
                    return CompletionContext.None;
                }
                
                if (LineStartsWith(currentLineTrimmed, new[] {"#", "|", "\""}))
                {
                    return CompletionContext.None;
                }
            }

            return SearchForContext(text, lineIndex, dialect);
        }

        private static GherkinDialect GetDialect(string featureLanguage)
        {
            GherkinDialect dialect;
            try
            {
                dialect = DialectProvider.GetDialect(featureLanguage, new Location());
            }
            catch (NoSuchLanguageException)
            {
                dialect = DialectProvider.DefaultDialect;
            }

            return dialect;
        }

        private static CompletionContext SearchForContext(in IList<string> text, in int lineIndex,
            in GherkinDialect dialect)
        {
            for (var i = lineIndex - 1; i >= 0; i--)
            {
                var line = text[i].Trim();
                var scenarioOutlineKeywords = dialect.ScenarioOutlineKeywords;
                var scenarioKeywords = dialect.ScenarioKeywords;
                var featureKeyword = dialect.FeatureKeywords;
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
            in GherkinDialect dialect)
        {
            var allNonStepKeywords = AllNonStepKeywords(dialect);
            return LineStartsWith(trimmedLine, allNonStepKeywords);
        }

        private static bool IsStepContext(string trimmedLine,
            in GherkinDialect dialect)
        {
            var allStepKeywords = dialect.StepKeywords;
            return LineStartsWith(trimmedLine, allStepKeywords);
        }

        private static bool LineStartsWith(string trimmedLine, IEnumerable<string> allKeywords)
        {
            return allKeywords.Select(keyword => keyword.Trim()).Any(trimmedLine.StartsWith);
        }

        private static IEnumerable<string> AllNonStepKeywords(in GherkinDialect dialect)
        {
            return dialect.FeatureKeywords
                .Union(dialect.BackgroundKeywords)
                .Union(dialect.ScenarioKeywords)
                .Union(dialect.ScenarioOutlineKeywords)
                .Union(dialect.ExamplesKeywords)
                .Select(word => word + ':');
        }

        private static readonly IDictionary<CompletionContext, Func<GherkinDialect, IEnumerable<string>>>
            ContextKeywordMapping =
                new Dictionary<CompletionContext, Func<GherkinDialect, IEnumerable<string>>>
                {
                    {
                        CompletionContext.Root,
                        dialect => dialect.FeatureKeywords
                            .Select(word => word + ':')
                    },
                    {
                        CompletionContext.Feature,
                        dialect => dialect.ScenarioKeywords
                            .Union(dialect.ScenarioOutlineKeywords)
                            .Union(dialect.BackgroundKeywords)
                            .Select(word => word + ':')
                    },
                    {
                        CompletionContext.Scenario,
                        dialect => dialect.ScenarioKeywords
                            .Union(dialect.ScenarioOutlineKeywords)
                            .Union(dialect.BackgroundKeywords)
                            .Select(word => word + ':')
                            .Union(dialect.StepKeywords)
                    },
                    {
                        CompletionContext.ScenarioOutline,
                        dialect => dialect.ScenarioKeywords
                            .Union(dialect.ScenarioOutlineKeywords)
                            .Union(dialect.BackgroundKeywords)
                            .Union(dialect.ExamplesKeywords)
                            .Select(word => word + ':')
                            .Union(dialect.StepKeywords)
                    },
                    {
                        CompletionContext.Step,
                        dialect => Enumerable.Empty<string>()
                    },
                    {
                        CompletionContext.None,
                        dialect => Enumerable.Empty<string>()
                    }
                };

        public static IEnumerable<string> GetAllKeywordsForContext(in CompletionContext context,
            in string language)
        {
            var dialect = GetDialect(language);
            return ContextKeywordMapping[context](dialect);
        }
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