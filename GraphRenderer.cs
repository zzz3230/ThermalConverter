using GraphSharp;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermalConverter
{
    internal class GraphRenderer
    {
        public static void RenderAndSaveToFile(ThermalConvert.Graph thGraph, string fileName)
        {
            ArgumentsHandler argz = new(fileName);

            var graph = CreateSampleGraph();
            graph.Do.MakeDirected();
            var points = graph.Do.FindArticulationPointsTarjan();
            foreach (var p in points)
            {
                //mark articulation points
                p.MapProperties().Color = Color.Aqua;
            }

            Helpers.CreateImage(argz, graph, drawer =>
            {
                drawer.Clear(Color.Black);
                drawer.DrawEdges(graph.Edges, argz.thickness, Color.DarkViolet);
                drawer.DrawNodes(graph.Nodes, argz.nodeSize);
                drawer.DrawNodeIds(graph.Nodes, Color.White, argz.fontSize);
            }, x => x.MapProperties().Position);

            GraphSharp.Graphs.Graph CreateSampleGraph()
            {
                Vector2 minv = thGraph.nodes.Values.First().pos;
                Vector2 maxv = thGraph.nodes.Values.First().pos;

                foreach (var node in thGraph.nodes)
                {
                    minv.x = Math.Min(minv.x, node.Value.pos.x);
                    minv.y = Math.Min(minv.y, node.Value.pos.y);
                    maxv.x = Math.Max(maxv.x, node.Value.pos.x);
                    maxv.y = Math.Max(maxv.y, node.Value.pos.y);
                }


                var graph = Helpers.CreateGraph(argz);
                graph.Edges.Clear();

                graph.Do.CreateNodes(thGraph.nodes.Count + 1);

                Dictionary<Guid, int> nodeId2Index = new();

                nodeId2Index[Guid.Empty] = 0;
                graph.Nodes[0].MapProperties().Position = DenseVector.OfArray([0, 0]);

                int index = 1;
                foreach (var node in thGraph.nodes)
                {
                    Vector2 normPos = (node.Value.pos - minv) / (maxv - minv);
                    nodeId2Index[node.Key] = index;
                    graph.Nodes[index++].MapProperties().Position = DenseVector.OfArray([(float)normPos.x, 1 - (float)normPos.y]);
                }

                foreach (var pipe in thGraph.pipes)
                {
                    graph.Edges.Add(new(graph.Nodes[nodeId2Index[pipe.Value.inputId]], graph.Nodes[nodeId2Index[pipe.Value.outputId]]));
                }

                return graph;
            }
        }
    }
}
