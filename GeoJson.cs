using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermalConverter;


public abstract class FeatureGeometry
{
    [JsonConverter(typeof(JsonStringEnumConverter<Type>))]
    public enum Type
    {
        Point,
        MultiLineString
    }
}

public class FeatureGeometryMultiLineString : FeatureGeometry
{        
    public Type type { get; set; }
    public List<List<Vector2>> coordinates { get; set; }
}
public class FeatureGeometryPoint : FeatureGeometry
{        
    public Type type { get; set; }
    public Vector2 coordinates { get; set; }
}

public class GeoJsonFeatureData
{
    public string type { get; set; }
    public Dictionary<string, object> properties { get; set; }
    
    [JsonConverter(typeof(FeatureGeometryConverter))]
    public FeatureGeometry geometry { get; set; }
}
public class GeoJsonData
{
    public string type { get; set; } = "";
    public string name { get; set; } = "";
    public object crs { get; } = new { type = "name", properties = new {name = "urn:ogc:def:crs:EPSG::3857"} };
    public List<GeoJsonFeatureData>  features { get; set; }
}


public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            var x = reader.GetDouble();
            reader.Read();
            var y = reader.GetDouble();
            reader.Read();
            
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                //reader.Read();
                return new (x, y);
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteRawValue($"[{value.x}, {value.y}]");
    }
}

public class FeatureGeometryConverter : JsonConverter<FeatureGeometry>
{
    public override FeatureGeometry Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var jsonObject = jsonDoc.RootElement;
            if (jsonObject.TryGetProperty("type", out JsonElement typeElement))
            {
                string? type = typeElement.GetString();
                switch (type)
                {
                    case "Point":
                        return JsonSerializer.Deserialize<FeatureGeometryPoint>(jsonObject.GetRawText(), options) ?? throw new JsonException();
                    case "MultiLineString":
                        return JsonSerializer.Deserialize<FeatureGeometryMultiLineString>(jsonObject.GetRawText(), options) ?? throw new JsonException();
                    default:
                        throw new JsonException($"Unknown type: {type}");
                }
            }
            else
            {
                throw new JsonException("Missing type discriminator field.");
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, FeatureGeometry value, JsonSerializerOptions options)
    {
        if (value is FeatureGeometryPoint pointGeo)
        {
            JsonSerializer.Serialize(writer, pointGeo, options);
        }
        else if (value is FeatureGeometryMultiLineString lineGeo)
        {
            JsonSerializer.Serialize(writer, lineGeo, options);
        }
        else
        {
            throw new JsonException();
        }
    }
}


public static class GeoJson
{
    public static GeoJsonData ReadFromFile(string fileName)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new Vector2JsonConverter()
            },
            WriteIndented = true
        };

        var text = File.ReadAllText(fileName);
        var geo = JsonSerializer.Deserialize<GeoJsonData>(text, options);
        Debug.Assert(geo != null);
        return geo;
    }

    public static void WriteToFile(string fileName, GeoJsonData data)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new Vector2JsonConverter()
            },
            WriteIndented = true
        };
        
        var geoJson = JsonSerializer.Serialize(data, options);
        
        File.WriteAllText(fileName, geoJson);
    }
}