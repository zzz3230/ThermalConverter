using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermalConverter;

public struct Vector2(double x, double y)
{
    public double x { get; set; } = x;
    public double y { get; set; } = y;

    public override string ToString()
    {
        return $"({x}, {y})";
    }

    public Vector2 MakeRound()
    {
        return new Vector2(Math.Round(x, 4), Math.Round(y, 4));
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
    public class Edge
    {
        public Guid inputId;
        public Guid outputId;
        public Vector2 inputPos;
        public Vector2 outputPos;
    }
    public class Node
    {
        public enum Type
        {
            Cross,
            Source,
            Consumer,
            Ctp
        }

        public Type type;
        public Vector2 pos;
    }
    
    public void Convert(string dataFolderPath)
    {
        var sections = GeoJson.ReadFromFile(Path.Combine(dataFolderPath, "section.geojson"));
        
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
        
        //points value is rawLines index
        Dictionary<Vector2, List<int>> points = new();
        Dictionary<Guid, Edge> edges = new();
        Dictionary<Guid, Node> nodes = new();
        
        List<Vector2[]> rawLines = new();

        
        
        for (int i = 0; i < sections.features.Count; i++)
        {
            var lines = sections.features[i].geometry as FeatureGeometryMultiLineString;
            Debug.Assert(lines != null);
            Debug.Assert(lines.coordinates.Count == 1); 

            for (int j = 0; j < lines.coordinates[0].Count - 1; j++)
            {
                var pointA = lines.coordinates[0][j];
                var pointB = lines.coordinates[0][j + 1];

                rawLines.Add([pointA, pointB]);
                
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

        List<Vector2> crosses = new();
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

        
        // foreach all points

        foreach (var point in points)
        {
            if (point.Value.Count == 2)
            {
                var line1Id = point.Value[0];
                var line2Id = point.Value[1];
                
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
                
                rawLines[line1Id][common1] = rawLines[line2Id][Rev(common2)];
                rawLines[line2Id][0] = new Vector2(0, 0);
                rawLines[line2Id][1] = new Vector2(0, 0);
                Console.WriteLine("Rm ln");
            }
        }
        
        

        int index = 100;
        GeoJsonData geo = new()
        {
            type = "FeatureCollection",
            name = "crosses",
            features = crosses.Select(cross => new GeoJsonFeatureData()
            {
                type = "Feature",
                properties = new Dictionary<string, object>() {{"Sys", index++}},
                geometry = new FeatureGeometryPoint() {coordinates = cross, type = FeatureGeometry.Type.Point}
            }).ToList()
        };

        GeoJson.WriteToFile(Path.Combine(dataFolderPath, "generated.geojson"), geo);
        
        return;
    }
}