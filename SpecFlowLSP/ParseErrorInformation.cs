namespace SpecFlowLSP
{
    public class ParseErrorInformation
    {
        public ParseErrorInformation(in string message, in Range range)
        {
            Message = message;
            Range = range;
        }

        public string Message { get; }
        public Range Range { get; }
    }
}