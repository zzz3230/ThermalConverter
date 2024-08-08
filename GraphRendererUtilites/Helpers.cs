using System.Diagnostics;
using GraphSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using GraphSharp.Graphs;
using Newtonsoft.Json;
using SampleBase;
using System.Text;
using GraphSharp.Common;
using GraphSharp.GraphDrawer;
using SixLabors.ImageSharp.Processing;
using MathNet.Numerics.LinearAlgebra.Single;

public static class Helpers
{
    public static void MeasureTime(Action operation)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine("Starting operation");
        Console.ResetColor();
        var watch = new Stopwatch();
        watch.Start();
        operation();
        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"End operation in {watch.ElapsedMilliseconds} Milliseconds");
        Console.ResetColor();
    }
    public static void PrintPath<TNode>(IList<TNode> path)
    where TNode : INode
    {
        System.Console.WriteLine("-------------------");
        foreach (var p in path)
        {
            System.Console.WriteLine(p);
        }
    }
    /// <summary>
    /// Normalizes all edges weights to be in range [0,1]
    /// </summary>
    /// <param name="edges"></param>
    public static void NormalizeEdgesWeights(IEnumerable<Edge> edges)
    {
        var max = edges.MaxBy(x => x.Weight)?.Weight ?? throw new ArgumentException("Null edge given");
        foreach (var e in edges)
            e.Weight /= max;
    }
    /// <summary>
    /// Normalizes positions of nodes to be in range [0,1]
    /// </summary>
    /// <param name="nodes"></param>
    public static void NormalizeNodePositions(IEnumerable<Node> nodes)
    {
        var maxX = 0f;
        var minX = float.MaxValue;
        var maxY = 0f;
        var minY = float.MaxValue;
        foreach (var n in nodes)
        {
            maxX = MathF.Max(n.MapProperties().Position[0], maxX);
            maxY = MathF.Max(n.MapProperties().Position[1], maxY);
            minX = MathF.Min(n.MapProperties().Position[0], minX);
            minY = MathF.Min(n.MapProperties().Position[1], minY);
        }
        var shift = new DenseVector(new[]{minX, minY});
        foreach (var n in nodes)
        {
            n.MapProperties().Position = (Vector)(n.MapProperties().Position- shift);
        }
        var diffX = maxX - minX;
        var diffY = maxY - minY;

        var maxDiff = MathF.Max(diffX, diffY);
        foreach (var n in nodes)
        {
            n.MapProperties().Position = (Vector)(n.MapProperties().Position/maxDiff);
        }
    }
    static Vector Vec(float x, float y) => (Vector)(new DenseVector(new[]{x,y}));
    public static void ShiftNodesToFitInTheImage<TNode>(IEnumerable<TNode> nodes, Func<TNode,Vector> getPos, Action<TNode,Vector> setPos)
    where TNode : INode
    {
        foreach (var n in nodes)
        {
            var newPos = Vec(getPos(n)[0] * 0.9f + 0.05f, getPos(n)[1] * 0.9f + 0.05f);
            setPos(n, newPos);
        }
    }
    public static void CreateImage<TNode, TEdge>(ArgumentsHandler argz, IImmutableGraph<TNode, TEdge> graph, Action<GraphDrawer<TNode, TEdge>> draw,Func<TNode,Vector> getPos)
    where TNode : INode
    where TEdge : IEdge
    {
        MeasureTime(() =>
        {
            System.Console.WriteLine("Creating image...");

            using var image = new Image<Rgba32>(argz.outputResolution, argz.outputResolution);
            image.Mutate(x =>
            {
                var shapeDrawer = new ImageSharpShapeDrawer(x, image, argz.fontSize);
                var drawer = new GraphDrawer<TNode, TEdge>(graph, shapeDrawer,argz.outputResolution,getPos);
                draw(drawer);
            });
            System.Console.WriteLine("Saving image...");
            image.SaveAsJpeg(argz.filename);
        });
    }
    public static Graph CreateGraph(ArgumentsHandler argz)
    {
        Graph? result = default;
        MeasureTime(() =>
        {
            System.Console.WriteLine("Creating graph...");
            var rand = new Random(argz.nodeSeed >= 0 ? argz.nodeSeed : new Random().Next());
            var conRand = new Random(argz.connectionSeed >= 0 ? argz.connectionSeed : new Random().Next());
            result = new Graph(id =>{
                var n = new Node(id); 
                n.MapProperties().Position = Vec(rand.NextSingle(), rand.NextSingle());
                n.MapProperties().Color=System.Drawing.Color.Red;
                return n;
            }, (n1, n2) => {
                var e = new Edge(n1, n2);
                e.MapProperties().Weight = (n1.MapProperties().Position - n2.MapProperties().Position).L2Norm();
                e.MapProperties().Color=System.Drawing.Color.DarkViolet;
                return e;
            });
            result.Configuration.Rand = conRand;
            result.Do.CreateNodes(argz.nodesCount);
            result.Do.ConnectToClosest(argz.minEdges, argz.maxEdges,(n1,n2)=>(n1.MapProperties().Position-n2.MapProperties().Position).L2Norm());
        });
        return result ?? throw new Exception("Create node failure"); ;
    }

    /// <summary>
    /// Save graph to json file
    /// </summary>
    public static void SaveGraph(Graph graph, string filename)
    {
        MeasureTime(() =>
        {
            System.Console.WriteLine("Saving graph...");
            var to_save = new
            {
                Nodes = graph.Nodes,
                Edges = graph.Edges
            };
            var json = JsonConvert.SerializeObject(to_save, Formatting.Indented);
            System.IO.File.WriteAllText(filename, json);
        });
    }

    //Load graph from json file
    public static Graph LoadGraph(string filename)
    {
        throw new NotImplementedException();
    }



}