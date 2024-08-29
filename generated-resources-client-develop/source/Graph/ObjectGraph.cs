using CSharpCustomExtensions.Collections.Dictionary;
using GeneratedResourceClient.GraphMaster.Tools;
using Nntc.ObjectModel;
using CSharpCustomExtensions.Flow;
using QuikGraph;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneratedResourceClient.Graph;

public class ObjectEdge : Edge<IDictionary<string, object>>
{
    public string TargetType { get; }
    public string SourceType { get; }
    public string PropertyName { get; }

    public ObjectEdge(IDictionary<string, object> source, IDictionary<string, object> target, string targetType, string sourceType, string propertyName) : base(source, target)
    {
        TargetType = targetType;
        SourceType = sourceType;
        PropertyName = propertyName;
    }

    public override string ToString()
    {
        return $"{SourceType} -> {TargetType}";
    }
}
public class PathFinder
{
    private readonly ObjectGraph _graph;
    private readonly IDictionary<string, object>? _targetVertex;

    public PathFinder(ObjectGraph graph, IDictionary<string, object>? targetVertex)
    {
        _graph = graph;
        _targetVertex = targetVertex;
    }

    public virtual PathFinder this[string targetType]
    {
        get
        {
            if (_targetVertex == null)
            {
                return this;
            }
            var edges = _graph.AdjacentEdges(_targetVertex!)!;
            var foundEdge = edges.FirstOrDefault(x => x.PropertyName == targetType || x.SourceType == targetType);

            if (foundEdge?.Source != _targetVertex)
                return new(_graph, foundEdge?.Source);
            else
                return new(_graph, foundEdge.Target);
        }
    }

    public IDictionary<string, object>? Get() => _targetVertex;
}
public class ReferenceJsonConvertor : JsonConverter<Reference>
{
    public override Reference Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => new Reference(reader.GetGuid(), null);
            

    public override void Write(
        Utf8JsonWriter writer,
        Reference reference,
        JsonSerializerOptions options) =>
            writer.WriteStringValue(reference.ToString());
}
public record Reference(Guid Id, string SubType)
{
    public override string ToString()
    {
        return Id.ToString();
    }

    public static implicit operator Guid (Reference reference) => reference.Id;
}

public partial class ObjectGraph : UndirectedGraph<IDictionary<string, object>, ObjectEdge>
{
    private readonly HashSet<IDictionary<string, object>> Items = new HashSet<IDictionary<string, object>>();
    public readonly Dictionary<string, List<IDictionary<string, object>>> TypedGroups = new();
    private const string Type = "type";
    private const string Id = "id";
    private const string SourceType = "InfluencedObject";
    private const string InfluencedObjectType = "InfluencedObjectType";

    public virtual PathFinder this[IDictionary<string, object> vertex] => new(this, vertex);
    public bool Contains(IDictionary<string, object> item) => Items.Contains(item);

    public void AddEdge(IDictionary<string, object> source, IDictionary<string, object> target, string propertyName)
    {
        var targetType = target[Type].ToString()!;
        var sourceType = source[Type].ToString()!;

        if (target.TryGetValue(InfluencedObjectType, out var targetInfluencedTypeType) && targetInfluencedTypeType.ToString()!.Equals(targetType))
        {
            propertyName = SourceType;
        }
        else if (source.TryGetValue(InfluencedObjectType, out var sourceInfluencedTypeType) && sourceInfluencedTypeType.ToString()!.Equals(targetType))
        {
            propertyName = SourceType;
        }

        var edge = new ObjectEdge(source, target, targetType, sourceType, propertyName);

        AddEdge(edge);
    }

    public new bool AddVertex(IDictionary<string, object> vertex)
    {
        var type = vertex[Type].ToString();
        Items.Add(vertex);
        TypedGroups!.GetOrCreate(type).Add(vertex);
        return base.AddVertex(vertex);
    }

    public new bool RemoveVertex(IDictionary<string, object> vertex)
    {
        var type = vertex[Type].ToString();
        Items.Remove(vertex);
        TypedGroups!.GetOrCreate(type).Remove(vertex);
        return base.RemoveVertex(vertex);
    }

    public virtual ObjectGraph Copy() => FillGraph(this, new ObjectGraph());
    public virtual ObjectGraph Merge(ObjectGraph second) => FillGraph(second, this);

    public virtual ObjectGraph FillGraph(ObjectGraph source, ObjectGraph target)
    {
        var pairs = new Dictionary<IDictionary<string, object>, IDictionary<string, object>>();

        foreach (var vertex in source.Vertices)
        {
            var newVertex = vertex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            pairs.Add(vertex, newVertex);
            target.AddVertex(newVertex);
        }

        foreach (var edge in source.Edges)
        {
            var newEdge = new ObjectEdge(pairs[edge.Source], pairs[edge.Target], edge.TargetType, edge.SourceType, edge.PropertyName);
            target.AddEdge(newEdge);
        }

        return target;
    }


    public virtual TypedCollectionLinkedResult ToTypedCollection(Dictionary<string, ObjectType> types, Func<ObjectType, ObjectType, List<ObjectRelationship>, ObjectRelationship> relationshipResolve) => ToTypedCollection(new TypeInfoResolver(types, relationshipResolve));
    public virtual TypedCollectionLinkedResult ToTypedCollection(Dictionary<string, ObjectType> types) => ToTypedCollection(new TypeInfoResolver(types, (type, objectType, relations) => relations.First()));

    public virtual TypedCollectionLinkedResult ToTypedCollection(TypeInfoResolver infoResolver) => ToTypedCollection((from, to, _, _, _) => infoResolver.GetInfo(from, to));

    public virtual TypedCollectionLinkedResult ToTypedCollection(Func<string, string, IDictionary<string, object>, IDictionary<string, object>, string, TypeInfo> getRelationType)
    {
        var relations = new Dictionary<(string, string), HashSet<Relation>>();

        void SetReference(ObjectEdge edge, string? influencedType)
        {
            // Получаем id исходного и целевого обьектов
            var sourceId = edge.Source.GetOrDefault(Id)?.ToString()?.Pipe(Guid.Parse!) ?? throw new Exception("Исходный обьект не содержит id");
            var targetId = edge.Target.GetOrDefault(Id)?.ToString()?.Pipe(Guid.Parse!) ?? throw new Exception("Целевой обьект не содержит id");

            // Получаем типы исходного и целевого обьектов
            var sourceType = edge.SourceType;
            var targetType = edge.TargetType;

            var referenceConfig = targetType switch
            {
                // Если тип равен родителькому типу процесса то берем InfluencedObject
                var type when type == influencedType => new TypeInfo((Multiplicity.One, Multiplicity.One), RelationshipDirection.Backward, (sourceType!, SourceType)),
                // Иначе получаем из схемы типов
                _ => getRelationType(sourceType!, targetType!, edge.Source, edge.Target, edge.PropertyName)
            };

            // Если связь многие ко многим то добавляем
            if (referenceConfig is ((Multiplicity.Many, Multiplicity.Many), RelationshipDirection.Forward, _))
            {
                var key = referenceConfig.TypeNames;
                if (!relations.ContainsKey(key)) relations.Add(key, new HashSet<Relation>());
                relations[key].Add(new Relation(sourceId, targetId));
            }
            else if (referenceConfig is ((Multiplicity.Many, Multiplicity.Many), RelationshipDirection.Backward, _))
            {
                var key = (referenceConfig.TypeNames.To, referenceConfig.TypeNames.From);

                if (!relations.ContainsKey(key)) relations.Add(key, new HashSet<Relation>());
                relations[key].Add(new Relation(targetId, sourceId));
            }
            else if (referenceConfig is ((Multiplicity.Many, Multiplicity.One), RelationshipDirection.Backward, _))
            {
                edge.Source[$"{referenceConfig.TypeNames.To}Id"] = new Reference(targetId, referenceConfig.TypeNames.From);
            }
            else if (referenceConfig is ((Multiplicity.One, Multiplicity.Many), RelationshipDirection.Forward, _))
            {
                edge.Source[$"{referenceConfig.TypeNames.To}Id"] = new Reference(targetId, referenceConfig.TypeNames.From);
            }
            // Много к одному в слукчае обратного направления, и один ко многим в случае прямого, для один к одному направление не важно
            else if (referenceConfig is ((Multiplicity.One, Multiplicity.One), _, _))
            {
                edge.Source[$"{referenceConfig.TypeNames.To}Id"] = new Reference(targetId, referenceConfig.TypeNames.From);
            }
            else if (referenceConfig is ((Multiplicity.Many, Multiplicity.One), RelationshipDirection.Forward, _))
            {
                edge.Target[$"{referenceConfig.TypeNames.From}Id"] = new Reference(sourceId, referenceConfig.TypeNames.To);
            }
            else if (referenceConfig is ((Multiplicity.One, Multiplicity.Many), RelationshipDirection.Backward, _))
            {
                edge.Target[$"{referenceConfig.TypeNames.From}Id"] = new Reference(sourceId, referenceConfig.TypeNames.To);
            }
        }

        var collection = new GeneratedResourcesCollection();

        foreach (var vertex in Vertices)
        {
            var type = vertex.GetOrDefault(Type)?.ToString() ?? throw new Exception("Теккущая обрабатываемая вершина не содержит тип");

            var influencedType = vertex.GetOrDefault(InfluencedObjectType)?.ToString();

            // Проходимся по ребрам текущего узла и добавляем а них ссылки в обьект

            foreach (var edge in AdjacentEdges(vertex))
            {
                SetReference(edge, influencedType);
            }

            if (!collection.ContainsKey(type))
            {
                collection[type] = new List<IDictionary<string, object>>();
            }

            collection[type].Add(vertex);
        }

        return new TypedCollectionLinkedResult(relations, collection);
    }
}