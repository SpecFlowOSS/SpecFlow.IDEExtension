using System;
using LspPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;

namespace SpecFlowLSP
{
    public static class PositionMapper
    {
        public static Position FromLspPosition(in LspPosition position)
        {
            return new Position(position.Line, position.Character);
        }

        public static LspLocation ToLspLocation(Location location)
        {
            return new LspLocation
            {
                Range = ToLspRange(location.Range),
                Uri = new Uri("file:/" + location.Path.Replace(@"\",@"/"))
            };
        }

        public static LspRange ToLspRange(Range range)
        {
            return new LspRange(ToLspPosition(range.Start), ToLspPosition(range.End));
        }

        public static LspPosition ToLspPosition(Position position)
        {
            return new LspPosition(position.Line, position.Character);
        }
    }
}