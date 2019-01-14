namespace SpecFlowLSP
{
    public class Position
    {
        public Position(in long line, in long character)
        {
            Line = line;
            Character = character;
        }

        public long Line { get; }
        public long Character { get; }
    }
}