using System.Diagnostics;

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
        return new Vector2(a.x - a.y, b.x - b.y);
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
    public class Pipe
    {
        public Guid inputId;
        public Guid outputId;
        public Vector2 inputPos;
        public Vector2 outputPos;
        public double length;
        public Guid uuid;
        public List<Guid> undirected = new();
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
    }

    public struct Line(Vector2 start, Vector2 end)
    {
        public Vector2 start { get; set; } = start;
        public Vector2 end { get; set; } = end;
        public double realLength { get; set; } = (end - start).Length();

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

    public void Convert(string dataFolderPath)
    {
        var sections = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "section.geojson"));
        var ctps = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "ctp.geojson"));
        var sources = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "sourse.geojson"));
        var consumers = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "consumer.geojson"));


        

        for (int i = 0; i < sections.features.Count; i++)
        {
            var lines = sections.features[i].geometry as FeatureGeometryMultiLineString;
            Debug.Assert(lines != null);
            Debug.Assert(lines.coordinates.Count == 1);

            for (int j = 0; j < lines.coordinates[0].Count; j++)
            {
                lines.coordinates[0][j] = lines.coordinates[0][j].MakeRound();
            }
        }


        var ctpsPointsSet = GeoJson.MakeSetOfThePoints(ctps);
        var sourcesPointsSet = GeoJson.MakeSetOfThePoints(sources);
        var consumersPointsSet = GeoJson.MakeSetOfThePoints(consumers);


        //points value is rawLines index
        Dictionary<Vector2, List<int>> points = new();
        Dictionary<Guid, Pipe> pipes = new();
        Dictionary<Guid, Node> nodes = new();

        List<Line> rawLines = new();

        for (int i = 0; i < sections.features.Count; i++)
        {
            var lines = sections.features[i].geometry as FeatureGeometryMultiLineString;
            Debug.Assert(lines != null);
            Debug.Assert(lines.coordinates.Count == 1);

            for (int j = 0; j < lines.coordinates[0].Count - 1; j++)
            {
                var pointA = lines.coordinates[0][j];
                var pointB = lines.coordinates[0][j + 1];

                rawLines.Add(new Line(pointA, pointB));

                if (points.TryGetValue(pointA, out var va))
                    va.Add(rawLines.Count - 1);
                else
                    points[pointA] = [rawLines.Count - 1];


                if (points.TryGetValue(pointB, out var vb))
                    vb.Add(rawLines.Count - 1);
                else
                    points[pointB] = [rawLines.Count - 1];
            }
        }


        HashSet<Vector2> crosses = new();
        for (int i = 0; i < rawLines.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (points[rawLines[i][j]].Count == 2) // just polyline therefore make single
                {

                }
                if (points[rawLines[i][j]].Count > 2) // three or more edges
                {
                    crosses.Add(rawLines[i][j]);
                }

            }
        }

        Debug.Assert(rawLines.All(x => x[0] != Vector2.zero && x[1] != Vector2.zero));

        // foreach all points

        foreach (var point in points)
        {
            if (point.Value.Count == 2)
            {
                if (ctpsPointsSet.Contains(point.Key))
                {
                    continue;
                }

                var line1Id = point.Value[0];
                var line2Id = point.Value[1];

                Debug.Assert(rawLines[line1Id][0] != Vector2.zero && rawLines[line1Id][1] != Vector2.zero);

                Debug.Assert(rawLines[line2Id][0] != Vector2.zero && rawLines[line2Id][1] != Vector2.zero);

                // (line1, line2) -> line1

                int common1 = -1;
                int common2 = -1;

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (rawLines[line1Id][i] == rawLines[line2Id][j])
                        {
                            Debug.Assert(common1 == -1 && common2 == -1);
                            common1 = i;
                            common2 = j;
                        }
                    }
                }

                Debug.Assert(common1 != -1 && common2 != -1);

                int Rev(int x) => x == 0 ? 1 : 0;

                var curLine = rawLines[line1Id];

                curLine[common1] = rawLines[line2Id][Rev(common2)];
                curLine.realLength += rawLines[line2Id].realLength;

                rawLines[line1Id] = curLine;
                

                rawLines[line2Id] = new Line(Vector2.zero, Vector2.zero);

                foreach (var p in points)
                {
                    for (int i = 0; i < p.Value.Count; i++)
                    {
                        if (p.Value[i] == line2Id)
                            p.Value[i] = line1Id;
                    }
                }

                //Debug.Assert(rawLinesIdTable[line1Id] == 175);
                //rawLinesIdTable[line2Id] = rawLinesIdTable[line1Id];

                //Console.WriteLine("Rm ln");
            }
        }

        //rawLines.RemoveAll(x => x[0] == Vector2.zero || x[1] == Vector2.zero);


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
                Debug.Assert(pointType == Node.Type.Unknown);
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

            //Debug.Assert(pointType != Node.Type.Unknown);
            if (pointType == Node.Type.Unknown) { continue; }

            Guid nodeId = Guid.NewGuid();
            nodes.Add(nodeId, new Node { pos = point.Key, type = pointType, uuid = nodeId });

            for (int i = 0; i < point.Value.Count; i++) 
            {
                if (convertedRawLines.ContainsKey(point.Value[i]))
                {
                    pipes[convertedRawLines[point.Value[i]]].undirected.Add(nodeId);
                }
                else
                {
                    Guid pipeId = Guid.NewGuid();
                    pipes.Add(pipeId, new Pipe { uuid = pipeId, length = rawLines[point.Value[i]].realLength });
                    pipes[pipeId].undirected.Add(nodeId);

                    convertedRawLines.Add(point.Value[i], pipeId);
                }
            }
        }

        //Console.WriteLine(nodes.Count);
        //Console.WriteLine(pipes.Count);
        //Console.WriteLine(points.Count);
        //Console.WriteLine(rawLines.Count);


        ReportGenerator.SavePipes([.. pipes.Values]);
        ReportGenerator.SaveNodes([.. nodes.Values]);


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

        return;
    }
}