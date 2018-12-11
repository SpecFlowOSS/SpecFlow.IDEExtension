using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gherkin;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;

namespace SpecFlowLSP
{
    public class CsharpDocumentHandler : ITextDocumentSyncHandler, ICompletionHandler
    {
        private readonly ILanguageServer _router;
        private readonly GherkinManager _manager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.cs",
            }
        );

        private SynchronizationCapability _capability;

        public CsharpDocumentHandler(in ILanguageServer router, in GherkinManager manager)
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
            _manager.HandleCsharpFileChanged(path, text);
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
            return Task.CompletedTask;
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
            return new TextDocumentAttributes(uri, "csharp");
        }
        public Task<CompletionList> Handle(TextDocumentPositionParams request, CancellationToken token)
        {
            return Task.FromResult(new CompletionList());
        }

       

        CompletionRegistrationOptions IRegistration<CompletionRegistrationOptions>.GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false,
                TriggerCharacters = new string[]{}
            };
        }

        public void SetCapability(CompletionCapability capability)
        {
        }
        
    }
}