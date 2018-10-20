namespace SpecFlowLSP
{
    public class ErrorLocation
    {
        public ErrorLocation(in long line, in long character)
        {
            Line = line;
            Character = character;
        }

        public long Line { get; }
        public long Character { get; }
    }
}