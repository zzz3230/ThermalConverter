using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using static ThermalConverter.ThermalConvert;

namespace ThermalConverter
{
    internal static class ReportGenerator
    {
        private static int _maxObjectsCountPerMessage;
        public static int maxObjectsCountPerMessage 
        { 
            get => _maxObjectsCountPerMessage; 
            set 
            { 
                if (value <= 1) 
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than 1");
                _maxObjectsCountPerMessage = value; 
            } 
        }
        
        public static bool writeIndented { get; set; }

        public static void SaveAllNodes(Graph graph, string outputFileName)
        {
            
            
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();
            
            var nodeIndex = 0;

            int fileIndex = 0;

            for (;;)
            {
                List<object> inData = [];

                for (;;)
                {
                    if (nodeIndex >= nodes.Count)
                    {
                        break;
                    }
                    
                    
                    inData.Add(
                        new
                        {
                            isReference = false,
                            type = "Node",
                            rowId = -1,
                            data = new
                            {
                                globalId = nodes[nodeIndex].uuid.ToString(),
                                nameShortRu = $"Node {nodeIndex}",
                                node = nodes[nodeIndex].properties,
                                element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                            }
                        }
                        );
                    
                    inData.Add(
                        new
                        {
                            isReference = false,
                            type = "GraphNode",
                            rowId = -1,
                            data = new
                            {
                                globalId = nodes[nodeIndex].kafkaNodeId,
                                graphNode = new
                                {
                                    elementId = nodes[nodeIndex].uuid.ToString(),
                                    point = new
                                    {
                                        coordinates = new[] { nodes[nodeIndex].pos.x, nodes[nodeIndex].pos.y },
                                        type = "Point"
                                    }
                                }
                            }
                        }
                        );

                    

                    nodeIndex++;
                    if (maxObjectsCountPerMessage - inData.Count < 2) // need at lest two
                    {
                        break;
                    }
                }

                if (inData.Count == 0)
                    break;
                
                var outputData = MakeMessage(inData);

                var outputJson = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = writeIndented });
                File.WriteAllText($"{outputFileName}_{fileIndex}.json", outputJson);

                fileIndex++;
            }
        }

        public static void SaveGraphEdges(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Pipe> pipes = graph.pipes.Values.ToList();

            int fileIndex = 0;
            var pipeIndex = 0;
            
            for (int i = 0; i < pipes.Count / maxObjectsCountPerMessage + 1; i++)
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), pipes.Count);

                var outputData = MakeMessage(pipes[start..end].Select(pipe => new
                {
                    isReference = false,
                    type = "GraphEdge",
                    rowId = -1,
                    data = new
                    {
                        globalId = pipe.kafkaEdgeId,
                        graphEdge = new
                        {
                            elementId = pipe.uuid.ToString(),
                            nameShortRu = $"Pipeline {pipeIndex++}",
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
                }));

                var outputJson = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = writeIndented });
                File.WriteAllText($"{outputFileName}_{fileIndex}.json", outputJson);

                fileIndex++;
            }

        }


        public static void SaveGraphNodes(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();

            int fileIndex = 0;

            for (int i = 0; i < nodes.Count / maxObjectsCountPerMessage + 1; i++)
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), nodes.Count);

                var outputData = MakeMessage(nodes[start..end].Select(node => new
                {
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
                }));

                var outputJson = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = writeIndented });
                File.WriteAllText($"{outputFileName}_{fileIndex}.json", outputJson);

                fileIndex++;
            }

            
        }

        public static void SaveNodes(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();
            
            var nodeIndex = 0;

            int fileIndex = 0;

            for (int i = 0; i < nodes.Count / maxObjectsCountPerMessage + 1; i++)
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), nodes.Count);

                var outputData = MakeMessage(nodes[start..end].Select(node => new
                {
                    isReference = false,
                    type = "Node",
                    rowId = -1,
                    data = new
                    {
                        globalId = node.uuid.ToString(),
                        nameShortRu = $"Node {nodeIndex++}",
                        element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                    }
                }));

                var outputJson = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = writeIndented });
                File.WriteAllText($"{outputFileName}_{fileIndex}.json", outputJson);

                fileIndex++;
            }
            
        }

        public static void SavePipelines(Graph graph, string outputFileName)
        {
            List<Pipe> pipes = graph.pipes.Values.ToList();

            var pipeIndex = 0;

            int fileIndex = 0;

            for (int i = 0; i < pipes.Count / maxObjectsCountPerMessage + 1; i++) 
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), pipes.Count);

                var outputData = MakeMessage(
                    pipes[start..end].Select(pipe => new
                    {
                        isReference = false,
                        type = "Pipeline",
                        rowId = -1,
                        data = new
                        {
                            globalId = pipe.uuid.ToString(),
                            nameShortRu = $"Pipeline {pipeIndex++}",
                            pipeline = new Dictionary<string, object>()
                            {
                                { "clientID", "00000000-0000-0000-0000-000000000000" },
                                { "length", pipe.length.ToString(CultureInfo.InvariantCulture) },
                                
                                //sidewallThickness = 0.14,
                                //diameterInner = 0.87,
                                //diameterOuter = 0.95,
                            }.Concat(pipe.properties).ToDictionary(x => x.Key, x => x.Value),
                            element = new { gatheringNetworkId = "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a" }
                        }
                    })
                    );

                var outputJson = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = writeIndented });
                File.WriteAllText($"{outputFileName}_{fileIndex}.json", outputJson);


                fileIndex++;
            }
        }

        static object MakeMessage(object data)
        {
            return new
            {
                MessageId = "c3b5d91c-a98a-4088-878f-d5d08763c0be",
                ModelId = "6146a6e9-b00f-4522-9c3b-e007a2b87ba1",
                Data = data
            };
        }
    }
}
