using System.IO.Compression;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using ThermalConverter;
using Unchase.Satsuma.Adapters;
using Path = System.IO.Path;

namespace ThermalConverterWebApi;

[Controller]
public class IndexController : Controller
{
    private readonly string _tempDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "temp");
    
    [HttpPost("convert")]
    public FileResult Convert(IFormFile consumerGeoJson, IFormFile ctpGeoJson, IFormFile sectionGeoJson, IFormFile sourceGeoJson)
    {
        Dictionary<ThermalConvert.UnitType, IFormFile> formFiles = new()
        {
            { ThermalConvert.UnitType.Consumer, consumerGeoJson },
            { ThermalConvert.UnitType.Ctp, ctpGeoJson },
            { ThermalConvert.UnitType.Section, sectionGeoJson },
            { ThermalConvert.UnitType.Source, sourceGeoJson },
        };

        if (!Directory.Exists(_tempDir))
            Directory.CreateDirectory(_tempDir);

        Dictionary<ThermalConvert.UnitType, string> filesNames = formFiles.Select(file =>
        {
            var fileName = Path.Combine(_tempDir, $"65ebc78e_{file.Key}.geojson");
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                file.Value.CopyTo(fs);
            }
            return (file.Key, fileName);
        }).ToDictionary(x => x.Key, x => x.fileName);

        var propMapFileName = Path.Combine(_tempDir, "property_map.json");
        if (!System.IO.File.Exists(propMapFileName))
        {
            System.IO.File.WriteAllText(propMapFileName, "{}");
        }
        
        ThermalConvert th = new(new(filesNames, propMapFileName));

        var graph = th.BuildGraph();

        var outDir = Path.Combine(_tempDir, "out");
        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        List<string> outFiles = [];
        outFiles.AddRange(ReportGenerator.SaveAllNodes(graph, Path.Combine(outDir, "all_nodes")));
        outFiles.AddRange(ReportGenerator.SavePipelines(graph, Path.Combine(outDir, "pipelines")));
        outFiles.AddRange(ReportGenerator.SaveGraphEdges(graph, Path.Combine(outDir, "graph_edges")));

        MemoryStream zipStream = new MemoryStream();
        var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);
        
        for (int i = 0; i < outFiles.Count; i++)
        {
            ZipArchiveEntry entry = archive.CreateEntry(Path.GetFileName(outFiles[i]), CompressionLevel.Fastest);
            using Stream stream = entry.Open();
            using FileStream fs = new FileStream(outFiles[i], FileMode.Open, FileAccess.Read);
            
            fs.CopyTo(stream);
        }
        zipStream.Position = 0;

        return File(zipStream, MediaTypeNames.Application.Zip, $"thermal_net_converted_{DateTime.Now:s}.zip");
    }
    
    [HttpPost("set_property_map")]
    public IActionResult Convert([FromBody] Dictionary<string, string>? propertyMap)
    {
        if (propertyMap == null)
        {
            return BadRequest();
        }
        var toWrite = System.Text.Json.JsonSerializer.Serialize(propertyMap, new JsonSerializerOptions() { WriteIndented = true });
        System.IO.File.WriteAllText(Path.Combine(_tempDir, "property_map.json"), toWrite);
        return Content("Written: \n" + toWrite);
    }
}