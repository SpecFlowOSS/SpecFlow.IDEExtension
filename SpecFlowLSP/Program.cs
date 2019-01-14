using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

namespace SpecFlowLSP
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            /*while (!System.Diagnostics.Debugger.IsAttached)
            {
                await Task.Delay(100);
            }*/


            var manager = new GherkinManager();
            var server = await LanguageServer.From(options =>
            {
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithServices(serviceCollection => serviceCollection.AddSingleton(manager))
                    .WithHandler<GherkinDocumentHandler>()
                    .OnInitialize(request =>
                    {
                        manager.HandleStartup(UrlSanitizer.SanitizeUrl(request.RootUri.OriginalString));
                        return Task.CompletedTask;
                    });
            });


            /*server.OnInitialize(request =>
            {
                manager.HandleStartup(UrlSanitizer.SanitizeUrl(request.RootUri.OriginalString));
                return Task.CompletedTask;
            });*/

            await server.WaitForExit;
        }
    }
}