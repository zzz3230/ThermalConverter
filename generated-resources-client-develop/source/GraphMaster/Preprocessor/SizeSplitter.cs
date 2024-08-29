using CSEx.Json.Extensions;
using CSharpCustomExtensions.Collections;
using CSharpCustomExtensions.Flow;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Tools;
using Nntc.ObjectModel;
using System.Collections;

namespace GeneratedResourceClient.GraphMaster.Preprocessor;

public static class SizeTool
{
    public static long GetFullSize(object obj)
    {
        var size = GetSize(obj);
        return size;//+ (long)((double)size * .28);
    }

    public static long GetSize(object obj)
    {
        if (obj == null)
        {
            return 0;
        }


        if (obj is IEnumerable<object> enumerable)
        {
            var ct = enumerable.Count();
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            var value = enumerator.Current;
            var size = GetSize(value);
            var sum = size * ct;
            return sum + (long)(double)(sum * .1);
        }

        if (obj is IDictionary dict)
        {
            long sum = 0;

            var enumerator = dict.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var val = enumerator.Current;

                var vt = val.GetType();

                if (val is DictionaryEntry kvpd)
                {
                    sum += GetSize(kvpd.Key) + GetSize(kvpd.Value);
                }
            }

            return sum;
        }

        var type = obj.GetType();
        var props = type.GetProperties().ToDictionary(x => x.Name);
        var ienumerable = typeof(IEnumerable);

        if (props.Values.Any(p => ienumerable.IsAssignableFrom(p.PropertyType)))
        {
            long sum = 0;

            foreach (var prop in props)
            {
                var value = prop.Value.GetValue(obj);
                sum += GetSize(value);
            }

            return sum;
        }
        else
        {
            var text = obj.Pipe(JsonUtils.Serialize);
            var size = System.Text.ASCIIEncoding.Unicode.GetByteCount(text);
            return size;
        }
    }

}

public class SizeSplitter
{
    private readonly Dictionary<string, ObjectType> _types;
    private readonly GeneratedResourcesConvertor _gn;

    public SizeSplitter(Dictionary<string, ObjectType> types)
    {
        _types = types;
    }

    static double ToMB(double bytesSize) => bytesSize / 1024 / 1024;

    static double GetGroupSizeMB(string group, Dictionary<string, long> sizes) => ((double)sizes[group]).Pipe(ToMB);

    /// <summary>
    /// Разделяет граф обьектов на части, которые имеют размер не больше чем maxSize
    /// </summary>
    /// <param name="sourceGraph">Исходный граф</param>
    /// <param name="maxSize">Максимальный размер части</param>
    /// <returns></returns>
    public IEnumerable<ObjectGraph> Split(ObjectGraph sourceGraph, double maxSize = 40)
    {
        var generalGraph = sourceGraph.Copy();

        yield return generalGraph;

        var data = generalGraph.TypedGroups;

        var sizes = data.ToDictionary(x => x.Key, x => SizeTool.GetFullSize(x.Value));

        var excluded = GetExcluded(sizes);

        foreach (var group in excluded)
        {
            var size = GetGroupSizeMB(group, sizes);

            if (size > maxSize)
            {
                var parts = size / maxSize;

                var items = data[group];

                var count = items.Count;

                var partSize = (int)Math.Ceiling(count / parts); // Колличество обьектов в 1 сообщении

                var coppies = items.Select(x =>
                {
                    var item = new Dictionary<string, object>(x); // Копия пойдет в другое сообщение

                    item["needsMatch"] = false; // Для нее id не сопоставляем, он уже есть в базе

                    RemoveCollectionProperties(x); // У того который пойдет в пером сообщении удаляем все свойства с коллекциями

                    return item;

                }).SplitBy(partSize)
                  .Select(ToGraph);

                foreach (var graph in coppies)
                {
                    yield return graph;
                }
            }
        }
    }

    /// <summary>
    /// Конвертирует коллекцию словарей (процессов), в граф
    /// </summary>
    /// <param name="processes">Коллекция процессов</param>
    /// <returns>Граф на загрузку с теми же процессами</returns>
    private ObjectGraph ToGraph(List<Dictionary<string, object>> processes)
    {
        var graph = new ObjectGraph();

        foreach (var process in processes)
        {
            graph.AddVertex(process);
        }

        return graph;
    }
    /// <summary>
    /// Выбирает процессы которые будут исключены из основного сообщения
    /// </summary>
    /// <param name="sizes"></param>
    /// <returns></returns>
    private List<string> GetExcluded(Dictionary<string, long> sizes)
    {
        var orderedProcessesSizes = sizes
            .Where(x => _types.ContainsKey(x.Key) && _types[x.Key].Pipe(t => t.BaseType?.Name is "Process"))
            .ToDictionary(x => x.Key, x => ToMB(x.Value))
            .OrderByDescending(x => x.Value)
            .ToList();

        var skipped = 0;

        List<string> excluded = new List<string>();

        for (; skipped < orderedProcessesSizes.Count(); skipped++)
        {
            var current = orderedProcessesSizes[skipped];

            var size = sizes.Keys.Except(excluded).Sum(x => GetGroupSizeMB(x, sizes));

            if (size < 20)
            {
                break;
            }

            excluded.Add(current.Key);
        }

        return excluded;
    }

    /// <summary>
    /// Удаляет значения которые являются коллекциями из обьекта
    /// </summary>
    /// <param name="item"></param>
    private static void RemoveCollectionProperties(IDictionary<string, object> item)
    {
        foreach (var kvp in item)
        {
            if (kvp.Value is IEnumerable<object> && kvp.Value is not string)
            {
                item[kvp.Key] = null;
            }
        }
    }
}
