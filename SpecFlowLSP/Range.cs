namespace SpecFlowLSP
{
    public class Range
    {
        public Range(in Position start, in Position end)
        {
            Start = start;
            End = end;
        }


        public Position Start { get; }
        public Position End { get; }
    }
}