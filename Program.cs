﻿using Microsoft.Extensions.Logging;
using ThermalConverter;

string dataFolderPath = "C:\\Users\\zzz32\\Downloads\\Telegram Desktop\\Теплосети Бердск\\geojson";

ThermalConvert.Args thArgs = new(
    new(){
        { ThermalConvert.UnitType.Section, Path.Combine(dataFolderPath, "section.geojson") },
        { ThermalConvert.UnitType.Ctp, Path.Combine(dataFolderPath, "ctp.geojson") },
        { ThermalConvert.UnitType.Source, Path.Combine(dataFolderPath, "sourse.geojson") }, // в 'Теплосети Бердск'/sourse.geojson опечатка 
        { ThermalConvert.UnitType.Consumer, Path.Combine(dataFolderPath, "consumer.geojson") }
    }, 
    "property_map.json"
);

var tc = new ThermalConvert(thArgs);
var thGraph = tc.BuildGraph();


// Save graph preview
//GraphRenderer.RenderAndSaveToFile(thGraph, "output2.jpg");

var maxObjectsCountStr = 
    Environment.GetEnvironmentVariable("ThermalConverter.maxObjectsCountPerMessage", EnvironmentVariableTarget.User)
    ?? "400";
if(int.TryParse(maxObjectsCountStr, out var maxObjectsCount))
{
    ReportGenerator.maxObjectsCountPerMessage = maxObjectsCount;
}
else
{
    throw new ArgumentException("ThermalConverter.maxObjectsCountPerMessage");
}


// var res = ReportGenerator.MakeGraphEdges(thGraph);
//
// ILoggerFactory loggerFactory = LoggerFactory.Create(x => x.AddConsole());
//
// var sender = new ReportSender(loggerFactory, "localhost:29092");
//
// foreach (var type in new string[] { "Node", "GraphNode", "Pipeline", "GraphEdge"})
// {
//     if(res.customParams.TryGetValue(type, out var param))
//         sender.AddType(type, param);
// }
//
// sender.SetupUploader(loggerFactory, "testing");
// for (int i = 0; i < res.data.Count; i++)
// {
//     sender.Upload(res.data[i]);
// }

return;

// Save kafka
var outDir = "out";
if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

//ReportGenerator.writeIndented = true;

// ReportGenerator.SaveNodes(thGraph,      Path.Combine(outDir, "node_kafka"));
 ReportGenerator.SavePipelines(thGraph,  Path.Combine(outDir, "pipeline_kafka"));
// ReportGenerator.SaveGraphNodes(thGraph, Path.Combine(outDir, "graph_node_kafka"));
 ReportGenerator.SaveGraphEdges(thGraph, Path.Combine(outDir, "graph_edge_kafka"));

ReportGenerator.SaveAllNodes(thGraph, Path.Combine(outDir, "all_nodes_kafka"));