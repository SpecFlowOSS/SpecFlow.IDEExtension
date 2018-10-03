'use strict';
'use strict';

import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions
} from 'vscode-languageclient';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	let exe = "dotnet";
	let serverModule = context.asAbsolutePath(
		path.join('out', 'server', 'SpecFlowLSP.dll')
	);

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	
	let serverOptions = {
		run: { 
			command: exe, 
			args: [serverModule] 
		},
		debug: {
			command: exe, 
			args: [serverModule]
		}
	};

	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{ scheme: 'file', language: 'gherkin' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
		}
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'languageServerExample',
		'Language Server Example',
		serverOptions,
		clientOptions
	);

	// Start the client. This will also launch the server
	client.start();
}

export function deactivate() {
	if (client) {
		client.stop();
	}
}