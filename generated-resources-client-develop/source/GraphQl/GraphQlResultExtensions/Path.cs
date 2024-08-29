using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratedResourceClient.GraphQl.GraphQlResultExtensions
{
    public class Path
    {
        public static string SegmentSeparator = ".";

        private List<Segment> _segments = new List<Segment>();

        public IReadOnlyList<Segment> GetSegments() => _segments.AsReadOnly();

        public bool IsSimple => _segments.Count <= 0 || _segments.Count == 1 && !_segments[0].IsAnyKey && !_segments[0].IsAnyKey;


        private Path()
        {

        }

        public Path(string pathStr) : this()
        {
            if (pathStr == null)
            {
                return;
            }

            _segments.AddRange(pathStr.Split(SegmentSeparator, StringSplitOptions.RemoveEmptyEntries).Select(i => new Segment(i)));
            if (_segments.Count > 0 && _segments.Last().IsAnyPath)
            {
                throw new ArgumentException("Последний сегмент пути поиска не может быть рекурсивным");
            }
        }

        public Path CreateSubPath(int segmentIndex)
        {
            var subPath = new Path();
            for (int i = segmentIndex; i < _segments.Count; ++i)
            {
                subPath._segments.Add(_segments[i]);
            }

            return subPath;
        }

        public static implicit operator Path(string value)
        {
            return new Path(value);
        }

        public Path CreateCombinedPath(Path subpath)
        {
            var combinedPath = new Path();
            combinedPath._segments.AddRange(_segments);
            combinedPath._segments.AddRange(subpath._segments);
            return combinedPath;
        }

        public Path CreateRelativePath(Path pathStart)
        {
            if (pathStart._segments.Count > _segments.Count)
            {
                throw new ArgumentException("Начальный путь не может содержать больше сегментов, чем частичный");
            }

            int i;
            for (i = 0; i < pathStart._segments.Count; ++i)
            {
                if (pathStart._segments[i] != _segments[i])
                {
                    throw new ArgumentException("Переданные пути не могут быть сопоставлены");
                }
            }

            var res = new Path();
            res._segments.AddRange(_segments.GetRange(i, _segments.Count - i));
            return res;
        }

        public override string ToString()
        {
            return string.Join(SegmentSeparator, _segments);
        }
    }
}
