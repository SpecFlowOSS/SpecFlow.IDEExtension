using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gherkin;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

namespace SpecFlowLSP
{
    public class GherkinDocumentHandler : ITextDocumentSyncHandler, ICompletionHandler, IDefinitionHandler,
        IReferencesHandler
    {
        private readonly ILanguageServer _router;
        private readonly GherkinManager _manager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.feature"
            }
        );

        public GherkinDocumentHandler(in ILanguageServer router, in GherkinManager manager)
        {
            _router = router;
            _manager = manager;
        }

        public TextDocumentSyncOptions Options { get; } = new TextDocumentSyncOptions()
        {
            WillSaveWaitUntil = false,
            WillSave = true,
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions
            {
                IncludeText = true
            },
            OpenClose = true
        };

        public Task Handle(DidChangeTextDocumentParams notification)
        {
            var path = notification.TextDocument.Uri.AbsolutePath;
            var text = notification.ContentChanges.First().Text;
            var parserExceptions = _manager.HandleFileRequest(path, text);
            SendDiagnostic(notification.TextDocument, parserExceptions);

            return Task.CompletedTask;
        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.
            GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                SyncKind = Options.Change
            };
        }

        public void SetCapability(SynchronizationCapability capability)
        {
        }

        public Task Handle(DidOpenTextDocumentParams notification)
        {
            var path = notification.TextDocument.Uri.AbsolutePath;
            var text = notification.TextDocument.Text;
            var errorInformation = _manager.HandleFileRequest(path, text);
            SendDiagnostic(notification.TextDocument, errorInformation);

            return Task.CompletedTask;
        }

        private void SendDiagnostic(TextDocumentIdentifier textDocument,
            IEnumerable<ParseErrorInformation> errorInformation)
        {
            var diagnostics = errorInformation.Select(ToDiagnostic);
            _router.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Diagnostics = new Container<Diagnostic>(diagnostics),
                Uri = textDocument.Uri
            });
        }

        private static Diagnostic ToDiagnostic(ParseErrorInformation errorInformation)
        {
            return new Diagnostic
            {
                Message = errorInformation.Message,
                Severity = DiagnosticSeverity.Error,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                {
                    Start = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position
                    {
                        Line = errorInformation.Range.Start.Line,
                        Character = errorInformation.Range.Start.Character
                    },
                    End = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position
                    {
                        Line = errorInformation.Range.End.Line,
                        Character = errorInformation.Range.End.Character
                    }
                }
            };
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions
            {
                DocumentSelector = _documentSelector
            };
        }

        public Task Handle(DidCloseTextDocumentParams notification)
        {
            _manager.HandleCloseRequest(notification.TextDocument.Uri.AbsolutePath);
            return Task.CompletedTask;
        }

        public Task Handle(DidSaveTextDocumentParams notification)
        {
            return Task.CompletedTask;
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                IncludeText = Options.Save.IncludeText
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new TextDocumentAttributes(uri, "gherkin");
        }

        public Task<CompletionList> Handle(TextDocumentPositionParams request, CancellationToken token)
        {
            var filePath = Path.GetFullPath(request.TextDocument.Uri.AbsolutePath);
            var language = _manager.GetLanguage(filePath);
            var text = _manager.GetFile(filePath);
            var context = ContextResolver.ResolveContext(text, (int) request.Position.Line, language);
            var line = text[(int) request.Position.Line];

            var completionItems =
                context == CompletionContext.Step
                    ? GetStepCompletion(request, filePath, line, language)
                    : ToCompletionItem(ContextResolver.GetAllKeywordsForContext(context, language), line,
                        request.Position);


            return Task.FromResult(new CompletionList(completionItems));
        }

        private static IEnumerable<CompletionItem> ToCompletionItem(in IEnumerable<string> allKeywords, string line,
            OmniSharp.Extensions.LanguageServer.Protocol.Models.Position position)
        {
            return allKeywords
                .Where(keyword => keyword.ToLower().Contains(line.Trim().ToLower()))
                .Select(keyword => new CompletionItem
                {
                    Label = keyword,
                    Kind = CompletionItemKind.Keyword,
                    TextEdit = new TextEdit
                    {
                        NewText = keyword,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                        {
                            Start = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position
                            {
                                Line = position.Line,
                                Character = FileUtils.FindLineStart(line)
                            },
                            End = position
                        }
                    }
                })
                .ToList();
        }

        private IEnumerable<CompletionItem> GetStepCompletion(TextDocumentPositionParams request, string filePath,
            string line, GherkinDialect language)
        {
            var stepPart = GetStep(line, language);

            var stepCompletion = _manager.GetSteps()
                .Where(step => NotCurrentStep(step, filePath, request.Position.Line) && ContainsLine(step, stepPart))
                .Distinct(StepInfo.TextComparer)
                .Select(step => ToCompletionItem(step, request.Position, stepPart.Length));
            return stepCompletion;
        }

        public static string GetStep(string line, GherkinDialect language)
        {
            var allKeywords = string.Join("|", language.StepKeywords).Replace("*", "\\*");
            return Regex.Match(line, $"({allKeywords})(.*)").Groups[2].Value.Trim();
        }

        private static bool ContainsLine(in StepInfo step, in string stepPart)
        {
            return step.Text.Contains(stepPart);
        }

        private static bool NotCurrentStep(StepInfo step, string filePath, long line)
        {
            return step.Position.Start.Line != line || step.FilePath != filePath;
        }

        private static CompletionItem ToCompletionItem(StepInfo step,
            OmniSharp.Extensions.LanguageServer.Protocol.Models.Position position, int stepPartLength)
        {
            return new CompletionItem
            {
                Kind = CompletionItemKind.Value,
                Label = step.Text,
                TextEdit = new TextEdit
                {
                    NewText = step.Text,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                    {
                        Start = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position
                        {
                            Line = position.Line,
                            Character = position.Character - stepPartLength
                        },
                        End = position
                    }
                },
                CommitCharacters = new Container<string>("\n")
            };
        }

        CompletionRegistrationOptions IRegistration<CompletionRegistrationOptions>.GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false,
                TriggerCharacters = new[] {"\n"}
            };
        }

        public void SetCapability(CompletionCapability capability)
        {
        }


        Task<LocationOrLocations> IRequestHandler<TextDocumentPositionParams, LocationOrLocations>.Handle(
            TextDocumentPositionParams request, CancellationToken token)
        {
            var locations = GetLocations(request.Position, request.TextDocument.Uri.AbsolutePath);
            var lspLocations = locations.Select(PositionMapper.ToLspLocation);
            return Task.FromResult(new LocationOrLocations(lspLocations));
        }

        private IEnumerable<Location> GetLocations(
            OmniSharp.Extensions.LanguageServer.Protocol.Models.Position position, string path)
        {
            return _manager.GetLocations(PositionMapper.FromLspPosition(position), path);
        }

        public void SetCapability(DefinitionCapability capability)
        {
        }

        public Task<LocationContainer> Handle(ReferenceParams request, CancellationToken token)
        {
            var locations = GetLocations(request.Position, request.TextDocument.Uri.AbsolutePath);
            var lspLocations = locations.Select(PositionMapper.ToLspLocation);
            return Task.FromResult(new LocationContainer(lspLocations));
        }

        public void SetCapability(ReferencesCapability capability)
        {
        }
    }
}