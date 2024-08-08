using Newtonsoft.Json;

public class ArgumentsHandler
{
    public int nodeSeed = 0;
    public int connectionSeed = 123;
    public int nodesCount = 200;
    public int node1 = 0;
    public int node2 = 10;
    public int minEdges = 1;
    public int maxEdges = 4;
    public int steps = 100;
    public double thickness = 0.01f;
    public int outputResolution = 1500;
    public double nodeSize = 0.015f;
    public double fontSize = 0.012f;
    public string filename = "example.jpg";
    public double directionLength = 0.1f;
    /// <summary>
    /// Desired render intervals
    /// </summary>
    public int renderIntervalMilliseconds = 1000;
    /// <summary>
    /// Desired intervals between each compute iteration
    /// </summary>
    public int computeIntervalMilliseconds = 500;
    public ArgumentsHandler(string fileName)
    {
        nodeSize = 0.0005;
        nodeSeed = 3;
        connectionSeed = 10;
        nodesCount = 5;
        node1 = 32;
        node2 = 63;
        minEdges = 5;
        maxEdges = 0;
        thickness = 0.0003;
        steps = 3000;
        outputResolution = 10000;
        fontSize = 0.001;
        filename = fileName;
        directionLength = 0.01f;
        renderIntervalMilliseconds = 1000;
        computeIntervalMilliseconds = 500;
    }
}