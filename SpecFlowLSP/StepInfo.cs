using System;
using System.Collections.Generic;

namespace SpecFlowLSP
{
    public readonly struct StepInfo : IEquatable<StepInfo>
    {
        public string Text { get; }
        public string FilePath { get; }
        public int Line { get; }

        public StepInfo(in string text, in string filePath, in int line)
        {
            Text = text;
            FilePath = filePath;
            Line = line - 1;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StepInfo other && Equals(other);
        }

        public bool Equals(StepInfo other)
        {
            return string.Equals(Text, other.Text) && string.Equals(FilePath, other.FilePath) && Line == other.Line;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Text != null ? Text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FilePath != null ? FilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Line;
                return hashCode;
            }
        }

        public static bool operator ==(StepInfo left, StepInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StepInfo left, StepInfo right)
        {
            return !left.Equals(right);
        }

        private sealed class TextEqualityComparer : IEqualityComparer<StepInfo>
        {
            public bool Equals(StepInfo x, StepInfo y)
            {
                return string.Equals(x.Text, y.Text);
            }

            public int GetHashCode(StepInfo obj)
            {
                return (obj.Text != null ? obj.Text.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<StepInfo> TextComparer { get; } = new TextEqualityComparer();
    }
}