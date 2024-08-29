using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratedResourceClient.GraphQl.GraphQlResultExtensions
{
    public class Segment
    {
        public const string AnyKeyWildCard = "*";
        public const string AnyPathWildCard = "**";

        public bool IsKey { get; private set; }

        public bool IsAnyKey => IsKey && Key == AnyKeyWildCard;

        public bool IsAnyPath => IsKey && Key == AnyPathWildCard;

        public bool IsIndex { get; private set; }

        public string? Key { get; private set; }

        public int? Index { get; private set; }

        public Segment(string str)
        {
            if (str is null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.Contains(Path.SegmentSeparator))
            {
                throw new ArgumentException($"Сегмент пути не может содержать разделитель сегментов");
            }

            if (!int.TryParse(str, out var v))
            {
                IsKey = true;
                Key = str;
            }
            else
            {
                IsIndex = true;
                Index = v;
            }
        }

        public override string ToString()
        {
            return IsKey ? Key! : $"{Index}";
        }

        public override bool Equals(object? obj)
        {
            var seg = obj as Segment;
            return seg?.IsIndex == true && IsIndex && seg?.Index == Index
                || seg?.IsKey == true && IsKey && seg?.Key == Key;
        }

        public static bool operator ==(Segment seg1, Segment seg2) => seg1.Equals(seg2);
        public static bool operator !=(Segment seg1, Segment seg2) => !seg1.Equals(seg2);

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
