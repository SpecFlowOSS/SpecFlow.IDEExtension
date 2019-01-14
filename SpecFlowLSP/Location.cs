namespace SpecFlowLSP
{
    public struct Location
    {
        public Range Range { get; }
        public string Path { get; }

        public Location(Range range, string path)
        {
            Path = path;
            Range = range;
        }
    }
}