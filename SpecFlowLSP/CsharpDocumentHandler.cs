using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.Embedded.MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

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

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken cancellationToken)
        {
            var path = notification.TextDocument.Uri.AbsolutePath;
            _manager.HandleCsharpFileChanged(path);
            return Unit.Task;
        }

        public TextDocumentSyncKind Change => TextDocumentSyncKind.Full;

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

        public Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions
            {
                DocumentSelector = _documentSelector,
            };
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken cancellationToken)
        {
            _manager.HandleCloseRequest(notification.TextDocument.Uri.AbsolutePath);
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken cancellationToken)
        {
            return Unit.Task;
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
            return new TextDocumentAttributes(uri, "csharp");
        }
        
        public Task<CompletionList> Handle(CompletionParams request, CancellationToken token)
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