using ThermalConverter;


var tc = new ThermalConvert();

var thGraph = tc.BuidGraph(
    "C:\\Users\\zzz32\\Downloads\\Telegram Desktop\\Теплосети Бердск\\geojson"
    );


// Save graph preview
//GraphRenderer.RenderAndSaveToFile(thGraph, "output2.jpg");


// Save kafka
var outDir = "out";
if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

ReportGenerator.SaveNodes(thGraph,      Path.Combine(outDir, "node_kafka.json"));
ReportGenerator.SavePipeliens(thGraph,  Path.Combine(outDir, "pipeline_kafka.json"));
ReportGenerator.SaveGraphNodes(thGraph, Path.Combine(outDir, "graph_node_kafka.json"));
ReportGenerator.SaveGraphEdges(thGraph, Path.Combine(outDir, "graph_edge_kafka.json"));