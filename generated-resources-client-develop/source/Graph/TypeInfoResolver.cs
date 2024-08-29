using Nntc.ObjectModel;
using System.Collections.Concurrent;

namespace GeneratedResourceClient.Graph;
class DictionaryCache<TSource, TResult> : DictionaryCache<TSource, TResult, TSource>
{
    public DictionaryCache(Func<TSource, TResult> creationFunc) : base(creationFunc, source => source)
    {
    }
}

public interface ICache<in TSource, out TResult>
{
    TResult Get(TSource value);
}

class DictionaryCache<TSource, TResult, TKey> : ICache<TSource, TResult>
{
    private readonly Func<TSource, TResult> _creationFunc;
    private readonly Func<TSource, TKey> _uncialProperty;

    public DictionaryCache(Func<TSource, TResult> creationFunc, Func<TSource, TKey> uncialProperty)
    {
        _creationFunc = creationFunc;
        _uncialProperty = uncialProperty;
        _results = new ConcurrentDictionary<TKey, TResult>();
    }

    private readonly ConcurrentDictionary<TKey, TResult> _results;

    public TResult Get(TSource value)
    {
        var key = _uncialProperty(value);

        if (_results.ContainsKey(key))
            return _results[key];

        var res = _creationFunc(value);
        _results[key] = res;

        return res;
    }
}

public static class MetadataEx
{
    public static ObjectTypeWithBase WithBaseAndSubTypes(this ObjectType type)
    {
        var res = new ObjectTypeWithBase
        {
            Properties = GetValues(type, x => x.Properties).SelectMany(x => x).ToList(),
            Relationships = GetValues(type, x => x.Relationships).SelectMany(x => x).ToList(),
            RelationshipSet = new RelationshipSet()
        };
        res.RelationshipSet.Initialize(res.Relationships);
        return res;
    }
    static IEnumerable<T> GetValues<T>(ObjectType type, Func<ObjectType, T> getProperty)
    {
        var res = getProperty(type);

        if (res != null)
        {
            yield return res;
        }

        foreach (var subtype in type.IncludesSubtypes)
        {
            var subTypeProperties = GetValues<T>(subtype, getProperty).ToList();

            foreach (var subTypeProperty in subTypeProperties)
            {
                yield return subTypeProperty;
            }
        }

        if (type.BaseType != null)
        {
            var baseTypeProperties = GetValues(type.BaseType, getProperty).ToList();

            foreach (var subtypeProperty in baseTypeProperties)
            {
                yield return subtypeProperty;
            }
        }
    }
    public static ObjectTypeWithBase Combine(this ObjectTypeWithBase source, ObjectType subType)
    {
        var res = new ObjectTypeWithBase()
        {
            RelationshipSet = new RelationshipSet(),
            IncludesSubtypes = source.IncludesSubtypes.Concat(subType.IncludesSubtypes).ToList(),
            Properties = source.Properties.Concat(subType.Properties).ToList(),
            Relationships = source.Relationships.Concat(subType.Relationships).ToList(),
            UniqueConstraints = source.UniqueConstraints.Concat(subType.UniqueConstraints).ToList()
        };
        res.RelationshipSet.Initialize(res.Relationships);
        return res;
    }
}

public class TypeInfoResolver
{
    public Func<ObjectType, ObjectType, List<ObjectRelationship>, ObjectRelationship>? RelationshipResolve { get; }
    private Dictionary<string, ObjectType> _types;
    private DictionaryCache<(string, string), TypeInfo> Cache;
    private readonly ObjectType _objectType;

    public TypeInfoResolver(Dictionary<string, ObjectType> types, Func<ObjectType, ObjectType, List<ObjectRelationship>, ObjectRelationship> relationshipResolve)
    {
        RelationshipResolve = relationshipResolve;
        _types = types;
        Cache = new DictionaryCache<(string from, string to), TypeInfo>(x => getInfo(x.from, x.to));
        _objectType = _types["Object"];
    }

    IEnumerable<string> GetTypeNames(ObjectType? type)
    {
        if (type == null) yield break;

        yield return type.Name;

        foreach (var tn in type.IncludesSubtypes.Prepend(type.BaseType).SelectMany(GetTypeNames))
        {
            yield return tn;
        }
    }

    Multiplicity getMultiplicity(Target relationship)
    {
        return relationship.MultiplicityMax switch
        {
            null => Multiplicity.Many,
            _ => Multiplicity.One
        };
    }

    public TypeInfo GetInfo(string source, string target) => Cache.Get((source, target));

    /// <summary>
    /// Получает информацию о связи между типами
    /// </summary>
    /// <param name="source">Ссылющийся тип</param>
    /// <param name="target">Тип по ссылке</param>
    /// <returns></returns>
    private TypeInfo getInfo(string source, string target)
    {
        if (!_types.ContainsKey(source) || !_types.ContainsKey(target))
        {
            return new TypeInfo((Multiplicity.None, Multiplicity.None), RelationshipDirection.Undirected, (source, target));
        }
        var st = _types[source];
        var tt = _types[target];

        // Получаем список допустимых имен целевого типа
        var ttNames = GetTypeNames(tt).ToHashSet();
        var includeSubtypes = st.WithBaseAndSubTypes().Combine(_objectType);
        // Проходимся по всем связям исходного типа и ищем там целевой тип
        var relations = includeSubtypes.Relationships.Where(x => ttNames.Contains(x.Target.Type.Name)).Distinct().ToList();

        if (relations.Any())
        {
            var relation = relations.First();

            if (relations.Count > 1 && RelationshipResolve != null)
            {
                relation = RelationshipResolve(st, tt, relations);
            }

            if (relation.Direction is RelationshipDirection.Backward)
            {
                return new TypeInfo((getMultiplicity(relation.Relationship.Source), getMultiplicity(relation.Relationship.Target)), relation.Direction, (relation.Target.Reverse.NavigationName, relation.Target.NavigationName));
            }
            else
            {
                return new TypeInfo((getMultiplicity(relation.Relationship.Source), getMultiplicity(relation.Relationship.Target)), relation.Direction, (relation.Type.Name, relation.Target.NavigationName));
            }
        }

        if (st?.BaseType?.Name is "Process")
            return new TypeInfo((Multiplicity.One, Multiplicity.One), RelationshipDirection.Undirected, (source, "InfluencedObject"));

        return new TypeInfo((Multiplicity.None, Multiplicity.None), RelationshipDirection.Undirected, (source, target));
    }
}
