using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using GeneratedResourceClient.GraphMaster.Tools;
using static ThermalConverter.ThermalConvert;

using MakeResultData = 
    System.Collections.Generic.List<
        System.Collections.Generic.IDictionary<
            string, 
            System.Collections.Generic.List<System.Collections.Generic.IDictionary<string, object>>
        >
    >;

namespace ThermalConverter
{
    public class MakeResult
    {
        public MakeResultData data = new();
        public Dictionary<string, List<string>> customParams = new();
    }
    
    public static class ReportGenerator
    {
        private static int _maxObjectsCountPerMessage = 400;
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

        private const string NODE_OBJECT_NAME = "Node";
        private const string GRAPH_NODE_OBJECT_NAME = "GraphNode";
        private const string PIPELINE_OBJECT_NAME = "Pipeline";
        private const string EDGE_OBJECT_NAME = "GraphEdge";
        
        public static MakeResult MakeAllNodes(Graph graph)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();
            
            var nodeIndex = 0;

            MakeResult result = new();
            
            

            for (;;)
            {
                List<IDictionary<string, object>> inDataNodes = [];
                List<IDictionary<string, object>> inDataGraphNodes = [];
                
                for (;;)
                {
                    if (nodeIndex >= nodes.Count)
                    {
                        break;
                    }

                    inDataNodes.Add(new Dictionary<string, object>(nodes[nodeIndex].properties!)
                    {
                        {"id", nodes[nodeIndex].uuid.ToString()}, // globalId
                        {"NameShortRu", $"Node {nodeIndex}"},
                        {"type", NODE_OBJECT_NAME},
                        {"gatheringNetworkId", "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a"}
                    });
                    
                    inDataGraphNodes.Add(new Dictionary<string, object>()
                    {
                        {"id", nodes[nodeIndex].kafkaNodeId.ToString()}, // globalId
                        {"elementId", nodes[nodeIndex].uuid.ToString()},
                        {"point", new
                            {
                                coordinates = new[] { nodes[nodeIndex].pos.x, nodes[nodeIndex].pos.y },
                                type = "Point"
                            }
                        },
                        {"type", GRAPH_NODE_OBJECT_NAME}
                    });
                    

                    nodeIndex++;
                    if (maxObjectsCountPerMessage - (inDataNodes.Count + inDataGraphNodes.Count) < 2) // need at lest two
                    {
                        break;
                    }
                }

                if (inDataNodes.Count == 0)
                    break;
                
                result.customParams.TryAdd(NODE_OBJECT_NAME, nodes[0].properties.Select(x => x.Key).Concat(["NameShortRu"]).ToList());
                result.customParams.TryAdd(GRAPH_NODE_OBJECT_NAME, ["point", "elementId"]);
                
                result.data.Add(new Dictionary<string, List<IDictionary<string, object>>>()
                {
                    {NODE_OBJECT_NAME, inDataNodes},
                    {GRAPH_NODE_OBJECT_NAME, inDataGraphNodes}
                });

            }

            return result;
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="outputFileName"></param>
        /// <returns>Saved files name</returns>
        public static List<string> SaveAllNodes(Graph graph, string outputFileName)
        {
            List<ThermalConvert.Node> nodes = graph.nodes.Values.ToList();
            
            var nodeIndex = 0;

            int fileIndex = 0;

            List<string> savedFiles = [];

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
                savedFiles.Add($"{outputFileName}_{fileIndex}.json");

                fileIndex++;
            }

            return savedFiles;
        }

        public static MakeResult MakeGraphEdges(Graph graph)
        {
            List<Pipe> pipes = graph.pipes.Values.ToList();
            
            MakeResult result = new();
            
            var pipeIndex = 0;

            int fileIndex = 0;
            
            for (int i = 0; i < pipes.Count / maxObjectsCountPerMessage + 1; i++) 
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), pipes.Count);

                result.data.Add(new Dictionary<string, List<IDictionary<string, object>>>()
                {
                    {
                        EDGE_OBJECT_NAME, 
                        pipes[start..end].Select(pipe => (IDictionary<string, object>)new Dictionary<string, object>()
                            {
                                {"type", EDGE_OBJECT_NAME},
                                {"nameShortRu", $"Pipeline {pipeIndex++}"},
                                {"targetGraphNodeId", graph.nodes[pipe.outputId].kafkaNodeId.ToString()},
                                {"sourceGraphNodeId", graph.nodes[pipe.inputId].kafkaNodeId.ToString()},
                                {"lineString", new
                                {
                                    coordinates = pipe.realPath.Select(x => new[] { x.x, x.y }),
                                    type = "LineString"
                                }},
                                {"gatheringNetworkId", "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a"},
                                {"id", pipe.kafkaEdgeId.ToString()},
                                {"elementId", pipe.uuid.ToString()}
                            }
                        ).ToList()
                    }
                });
                
                fileIndex++;
            }
            
            if(result.data.Count > 0)
                result.customParams.Add(
                    EDGE_OBJECT_NAME, 
                    ["targetGraphNodeId", "sourceGraphNodeId", "NameShortRu", "lineString", "elementId"]
                );

            return result;
        }
        public static List<string> SaveGraphEdges(ThermalConvert.Graph graph, string outputFileName)
        {
            List<ThermalConvert.Pipe> pipes = graph.pipes.Values.ToList();

            int fileIndex = 0;
            var pipeIndex = 0;
            
            List<string> savedFiles = [];
            
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
                savedFiles.Add($"{outputFileName}_{fileIndex}.json");

                fileIndex++;
            }

            return savedFiles;
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

        public static MakeResult MakePipelines(Graph graph)
        {
            List<Pipe> pipes = graph.pipes.Values.ToList();
            
            MakeResult result = new();
            
            var pipeIndex = 0;

            int fileIndex = 0;
            
            for (int i = 0; i < pipes.Count / maxObjectsCountPerMessage + 1; i++) 
            {
                int start = maxObjectsCountPerMessage * fileIndex;
                int end = Math.Min(maxObjectsCountPerMessage * (fileIndex + 1), pipes.Count);

                result.data.Add(new Dictionary<string, List<IDictionary<string, object>>>()
                {
                    {
                        PIPELINE_OBJECT_NAME, 
                        pipes[start..end].Select(pipe => (IDictionary<string, object>)new Dictionary<string, object>(pipe.properties)
                        {
                            {"type", PIPELINE_OBJECT_NAME},
                            {"clientID", "00000000-0000-0000-0000-000000000000"},
                            {"nameShortRu", $"Pipeline {pipeIndex++}"},
                            {"length", pipe.length.ToString(CultureInfo.InvariantCulture) },
                            {"gatheringNetworkId", "ff225d04-d3ad-4a6c-a86a-a9f60b812a0a"},
                            {"id", pipe.uuid.ToString()}
                        }
                        ).ToList()
                    }
                });
                
                fileIndex++;
            }
            
            if(result.data.Count > 0)
                result.customParams.Add(
                    PIPELINE_OBJECT_NAME, 
                    pipes[0].properties.Select(x => x.Key).Concat(["clientID", "length", "NameShortRu"]).ToList()
                    );

            return result;
        }
        
        public static List<string> SavePipelines(Graph graph, string outputFileName)
        {
            List<Pipe> pipes = graph.pipes.Values.ToList();

            var pipeIndex = 0;

            int fileIndex = 0;
            
            List<string> savedFiles = [];

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
                savedFiles.Add($"{outputFileName}_{fileIndex}.json");

                fileIndex++;
            }

            return savedFiles;
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
