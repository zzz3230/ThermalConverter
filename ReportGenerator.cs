using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ThermalConverter
{
    internal static class ReportGenerator
    {
        public static void SaveGraphEdges(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Pipe> pipes = graph.pipes.Values.ToList();

            var outputData = new
            {
                MessageId = "c3b5d91c-a98a-4088-878f-d5d08763c0be",
                ModelId = "6146a6e9-b00f-4522-9c3b-e007a2b87ba1",
                Data = pipes.Select(pipe => new {
                    isReference = false,
                    type = "GraphEdge",
                    rowId = -1,
                    data = new
                    {
                        globalId = pipe.kafkaEdgeId,
                        graphEdge = new
                        {
                            elementId = pipe.uuid.ToString(),
                            targetGraphNodeId = graph.nodes[pipe.outputId].kafkaNodeId.ToString(),
                            sourceGraphNodeId = graph.nodes[pipe.inputId].kafkaNodeId.ToString(),
                            lineString = new
                            {
                                coordinates = pipe.realPath.Select(x => new[] { x.x, x.y }),
                                type = "LineString"
                            }
                        },
                        element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                    }
                })
            };

            var outputJson = JsonSerializer.Serialize(outputData);
            File.WriteAllText(outputFileName, outputJson);
        }


        public static void SaveGraphNodes(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();

            var outputData = new
            {
                MessageId = "c3b5d91c-a98a-4088-878f-d5d08763c0be",
                ModelId = "6146a6e9-b00f-4522-9c3b-e007a2b87ba1",
                Data = nodes.Select(node => new {
                    isReference = false,
                    type = "GraphNode",
                    rowId = -1,
                    data = new
                    {
                        globalId = node.kafkaNodeId,
                        graphNode = new
                        {
                            elementId = node.uuid.ToString(),
                            point = new
                            {
                                coordinates = new[] { node.pos.x, node.pos.y },
                                type = "Point"
                            }
                        }
                    }
                })
            };

            var outputJson = JsonSerializer.Serialize(outputData);
            File.WriteAllText(outputFileName, outputJson);
        }

        public static void SaveNodes(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();

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
            File.WriteAllText(outputFileName, outputJson);
        }

        public static void SavePipeliens(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Pipe> pipes = graph.pipes.Values.ToList();

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
                            //sidewallThickness = 0.14,
                            //diameterInner = 0.87,
                            //diameterOuter = 0.95,
                        },
                        element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                    }
                })
            };

            var outputJson = JsonSerializer.Serialize(outputData);
            File.WriteAllText(outputFileName, outputJson);
        }
    }
}
