using System.Collections.ObjectModel;
using System.Diagnostics;
using static ThermalConverter.ThermalConvert;

namespace ThermalConverter;

public struct Vector2(double x, double y)
{
    public static Vector2 zero = new Vector2(0, 0);

    public double x { get; set; } = x;
    public double y { get; set; } = y;


    public double Length()
    {
        return Math.Sqrt(x * x + y * y);
    }

    public override string ToString()
    {
        return $"({x}, {y})";
    }

    public Vector2 MakeRound()
    {
        return new Vector2(Math.Round(x, 4), Math.Round(y, 4));
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x - b.x, a.y - b.y);
    }

    public static Vector2 operator /(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x / b.x, a.y / b.y);
    }

    public static bool operator ==(Vector2 a, Vector2 b)
    {
        return Math.Abs(a.x - b.x) < double.Epsilon && Math.Abs(a.y - b.y) < double.Epsilon;
    }

    public static bool operator !=(Vector2 a, Vector2 b)
    {
        return !(a == b);
    }

}

public class ThermalConvert
{
    public class Graph()
    {
        public Dictionary<Guid, Pipe> pipes { get; } = new();
        public Dictionary<Guid, Node> nodes { get; } = new();
    }

    public class Pipe
    {
        public Guid inputId;
        public Guid outputId;
        public double length;
        public Guid uuid;
        public Guid kafkaEdgeId;
        public ReadOnlyCollection<Vector2> realPath;

        public override string ToString()
        {
            return $"in={inputId}; out={outputId}; length={length}; id={uuid};";
        }
    }
    public class Node
    {
        public enum Type
        {
            Unknown,
            Cross,
            Source,
            Consumer,
            Ctp
        }

        public Type type;
        public Vector2 pos;
        public Guid uuid;
        public Guid kafkaNodeId;
        public List<Guid> connectedPipesIds = new();

        public override string ToString()
        {
            return $"type={type}; pos={pos}; id={uuid}; pipes=[{string.Join(", ", connectedPipesIds)}];";
        }
    }

    public struct Line(Vector2 start, Vector2 end, double realLength, List<Vector2> realPath)
    {
        public Vector2 start { get; set; } = start;
        public Vector2 end { get; set; } = end;
        public double realLength { get; } = realLength;

        public ReadOnlyCollection<Vector2> realPath { get; } = realPath.AsReadOnly();

        public Vector2 this[int key]
        {
            get => key switch { 0 => start, 1 => end, _ => throw new IndexOutOfRangeException() };
            set { switch (key) { 
                    case 0: start = value; break; 
                    case 1: end = value; break; 
                    default: throw new IndexOutOfRangeException(); 
                }
            }
        }
    }

    public Graph BuidGraph(string dataFolderPath)
    {
        var sections = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "section.geojson"));
        var ctps = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "ctp.geojson"));
        var sources = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "sourse.geojson")); // в 'Теплосети Бердск'/sourse.geojson опечатка 
        var consumers = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "consumer.geojson"));

        //for (int i = 0; i < sections.features.Count; i++)
        //{
        //    var lines = sections.features[i].geometry as FeatureGeometryMultiLineString;
        //    Debug.Assert(lines != null);
        //    Debug.Assert(lines.coordinates.Count == 1);

        //    for (int j = 0; j < lines.coordinates[0].Count; j++)
        //    {
        //        lines.coordinates[0][j] = lines.coordinates[0][j].MakeRound();
        //    }
        //}


        var ctpsPointsSet = GeoJson.MakeSetOfThePoints(ctps);
        var sourcesPointsSet = GeoJson.MakeSetOfThePoints(sources);
        var consumersPointsSet = GeoJson.MakeSetOfThePoints(consumers);


        //points value is rawLines index
        Dictionary<Vector2, List<int>> points = new();

        Graph graph = new();
        var pipes = graph.pipes;
        var nodes = graph.nodes;

        List<Line> rawLines = new();

        for (int i = 0; i < sections.features.Count; i++)
        {
            var lines = sections.features[i].geometry as FeatureGeometryMultiLineString;
            Debug.Assert(lines != null);
            Debug.Assert(lines.coordinates.Count == 1);

            double length = 0;
            for (int j = 0; j < lines.coordinates[0].Count - 1; j++)
            {
                var pointA = lines.coordinates[0][j];
                var pointB = lines.coordinates[0][j + 1];

                length += (pointB - pointA).Length();
            }

            var beginPoint = lines.coordinates[0][0];
            var endPoint = lines.coordinates[0][^1];

            rawLines.Add(new Line(beginPoint, endPoint, length, lines.coordinates[0]));


            if (points.TryGetValue(beginPoint, out var va))
                va.Add(rawLines.Count - 1);
            else
                points[beginPoint] = [rawLines.Count - 1];


            if (points.TryGetValue(endPoint, out var vb))
                vb.Add(rawLines.Count - 1);
            else
                points[endPoint] = [rawLines.Count - 1];

        }

        HashSet<Vector2> crosses = new();
        for (int i = 0; i < rawLines.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (points[rawLines[i][j]].Count > 1) // one or more edges
                {
                    crosses.Add(rawLines[i][j]);
                }
            }
        }

        // int is rawLines index; Guid is pipes key
        Dictionary<int, Guid> convertedRawLines = new();

        foreach (var point in points)
        {
            Node.Type pointType = Node.Type.Unknown;

            if (crosses.Contains(point.Key))
            {
                Debug.Assert(pointType == Node.Type.Unknown);
                pointType = Node.Type.Cross;
            }

            if (ctpsPointsSet.Contains(point.Key))
            {
                Debug.Assert(pointType == Node.Type.Unknown || pointType == Node.Type.Cross);
                pointType = Node.Type.Ctp;
            }

            if (sourcesPointsSet.Contains(point.Key))
            {
                Debug.Assert(pointType == Node.Type.Unknown || pointType == Node.Type.Cross);
                pointType = Node.Type.Source;
            }

            if (consumersPointsSet.Contains(point.Key))
            {
                Debug.Assert(pointType == Node.Type.Unknown);
                pointType = Node.Type.Consumer;
            }

            if (pointType == Node.Type.Unknown) { continue; }

            Guid nodeId = Guid.NewGuid();
            nodes.Add(nodeId, new Node { pos = point.Key, type = pointType, uuid = nodeId, kafkaNodeId = Guid.NewGuid() });

            for (int i = 0; i < point.Value.Count; i++)
            {
                Guid pipeId;
                if (!convertedRawLines.ContainsKey(point.Value[i]))
                {
                    pipeId = Guid.NewGuid();
                    pipes.Add(pipeId, new Pipe { 
                        uuid = pipeId, 
                        length = rawLines[point.Value[i]].realLength, 
                        realPath = rawLines[point.Value[i]].realPath,
                        kafkaEdgeId = Guid.NewGuid(),
                    });

                    convertedRawLines.Add(point.Value[i], pipeId);
                }
                else
                {
                    pipeId = convertedRawLines[point.Value[i]];
                }

                nodes[nodeId].connectedPipesIds.Add(pipeId);

                if (point.Key == rawLines[point.Value[i]].start)
                    pipes[pipeId].inputId = nodeId;
                else if (point.Key == rawLines[point.Value[i]].end)
                    pipes[pipeId].outputId = nodeId;
                else
                    throw new Exception();
            }
        }


        // Пока что удаляю трубы без конца
        graph.pipes.RemoveAll(x => x.Value.outputId == Guid.Empty);


        //int index = 100;
        //GeoJsonData geo = new()
        //{
        //    type = "FeatureCollection",
        //    name = "lines",
        //    features = rawLines.Select(ln => new GeoJsonFeatureData()
        //    {
        //        type = "Feature",
        //        properties = new Dictionary<string, object>() { { "Sys", index++ } },
        //        geometry = new FeatureGeometryMultiLineString() { coordinates = [[ln[0], ln[1]]], type = FeatureGeometry.Type.MultiLineString }
        //    }).ToList()
        //};
        //GeoJson.WriteToFile(Path.Combine(dataFolderPath, "generated_lines.geojson"), geo);

        //int index = 100;
        //GeoJsonData geo = new()
        //{
        //    type = "FeatureCollection",
        //    name = "crosses",
        //    features = crosses.Select(cross => new GeoJsonFeatureData()
        //    {
        //        type = "Feature",
        //        properties = new Dictionary<string, object>() {{"Sys", index++}},
        //        geometry = new FeatureGeometryPoint() {coordinates = cross, type = FeatureGeometry.Type.Point}
        //    }).ToList()
        //};

        //GeoJson.WriteToFile(Path.Combine(dataFolderPath, "generated.geojson"), geo);

        return graph;
    }
}