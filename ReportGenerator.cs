using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ThermalConverter
{
    internal static class ReportGenerator
    {
        public static void SaveNodes(List<ThermalConvert.Node> nodes)
        {
            var nodeIndex = 0;
            var outputData = new
            {
                MessageId = "c3b5d91c-a98a-4088-878f-d5d08763c0be",
                ModelId = "6146a6e9-b00f-4522-9c3b-e007a2b87ba1",
                Data = nodes.Select(node => new {
                    isReference = false,
                    type = "Node",
                    rowId = -1,
                    data = new
                    {
                        globalId = node.uuid.ToString(),
                        nameShortRu = $"Node {nodeIndex++}",
                        element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                    }
                })
            };

            var outputJson = JsonSerializer.Serialize(outputData);

            File.WriteAllText("test_report_nodes.json", outputJson);
        }

        public static void SavePipes(List<ThermalConvert.Pipe> pipes)
        {
            var pipeIndex = 0;
            var outputData = new
            {
                MessageId = "c3b5d91c-a98a-4088-878f-d5d08763c0be",
                ModelId = "6146a6e9-b00f-4522-9c3b-e007a2b87ba1",
                Data = pipes.Select(pipe => new {
                    isReference = false,
                    type = "Pipeline",
                    rowId = -1,
                    data = new
                    {
                        globalId = pipe.uuid.ToString(),
                        nameShortRu = $"Pipeline {pipeIndex++}",
                        pipeline = new 
                        {
                            clientID = "00000000-0000-0000-0000-000000000000",
                            length = pipe.length,
                            sidewallThickness = 0.14,
                            diameterInner = 0.87,
                            diameterOuter = 0.95,
                            con1 = pipe.undirected[0],
                            con2 = pipe.undirected.Count == 2 ? pipe.undirected[1] : Guid.Empty,
                        },
                        element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                    }
                })
            };

            var outputJson = JsonSerializer.Serialize(outputData);

            File.WriteAllText("test_report_pipes.json", outputJson);
        }
    }
}
