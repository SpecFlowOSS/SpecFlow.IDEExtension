using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlowLSP.Client.VisualStudio
{
    [ContentType("gherkin")]
    [Export(typeof(ILanguageClient))]
    public class GherkinLanguageClient : ILanguageClient
    {
        public string Name => "Gherkin Language Extension";
        public IEnumerable<string> ConfigurationSections => null;
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => null;
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public async  Task OnLoadedAsync()
        {
            await StartAsync?.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var wd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var serverDir = Path.Combine(wd, "Server");
            var info = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = serverDir,
                UseShellExecute = false,
                ErrorDialog = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = ".\\SpecFlowLSP.dll"
            };

            var process = new Process {StartInfo = info};

            return process.Start() ? new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream) : null;
        }

    }
}
