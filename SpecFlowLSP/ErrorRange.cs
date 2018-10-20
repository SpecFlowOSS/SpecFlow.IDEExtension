namespace SpecFlowLSP
{
    public class ErrorRange
    {
        public ErrorRange(in ErrorLocation start, in ErrorLocation end)
        {
            Start = start;
            End = end;
        }


        public ErrorLocation Start { get; }
        public ErrorLocation End { get; }
    }
}