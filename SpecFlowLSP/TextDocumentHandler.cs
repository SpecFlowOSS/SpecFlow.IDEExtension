using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

namespace SpecFlowLSP
{
    class TextDocumentHandler : ITextDocumentSyncHandler, ICompletionHandler
    {
        private readonly ILanguageServer _router;
        private readonly GherkinManager _manager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.feature",
                Language = "gherkin"
            }
        );

        private SynchronizationCapability _capability;

        public TextDocumentHandler(in ILanguageServer router, in GherkinManager manager)
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
            _capability = capability;
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
                Range = new Range
                {
                    Start = new Position
                    {
                        Line = errorInformation.Range.Start.Line,
                        Character = errorInformation.Range.Start.Character
                    },
                    End = new Position
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
                DocumentSelector = _documentSelector,
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
            return new TextDocumentSaveRegistrationOptions()
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

            IEnumerable<CompletionItem> completionItems;

            if (context == CompletionContext.Step)
            {
                completionItems = GetStepCompletion(request, filePath);
            }
            else
            {
                completionItems = ToCompletionItem(ContextResolver.GetAllKeywordsForContext(context, language));
            }



            return Task.FromResult(new CompletionList(completionItems));
        }

        private static IEnumerable<CompletionItem> ToCompletionItem(IEnumerable<string> allKeywords)
        {
            return allKeywords.Select(keyword => new CompletionItem {Label = keyword});
        }

        private IEnumerable<CompletionItem> GetStepCompletion(TextDocumentPositionParams request, string filePath)
        {
            var stepCompletion = _manager.GetSteps()
                .Where(step => NotCurrentStep(step, filePath, request.Position.Line))
                .Distinct(StepInfo.TextComparer)
                .Select(ToCompletionItem);
            return stepCompletion;
        }

        private static bool NotCurrentStep(StepInfo step, string filePath, long line)
        {
            return step.Line != line || step.FilePath != filePath;
        }

        private static CompletionItem ToCompletionItem(StepInfo step)
        {
            return new CompletionItem
            {
                Kind = CompletionItemKind.Value,
                Label = step.Text,
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
    }
}