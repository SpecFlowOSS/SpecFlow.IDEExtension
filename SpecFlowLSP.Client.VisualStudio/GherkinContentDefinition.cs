using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlowLSP.Client.VisualStudio
{
    public class GherkinContentDefinition
    {
        [Export] [Name("gherkin")] [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition GherkinContenTypeDefiniition;

        [Export] [FileExtension(".feature")] [ContentType("gherkin")]
        internal static FileExtensionToContentTypeDefinition GherkinFileExtensionDefinition;
    }
}