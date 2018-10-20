namespace SpecFlowLSP
{
    public class ParseErrorInformation
    {
        public ParseErrorInformation(in string message, in ErrorRange range)
        {
            Message = message;
            Range = range;
        }

        public string Message { get; }
        public ErrorRange Range { get; }
    }
}