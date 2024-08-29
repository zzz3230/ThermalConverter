using GeneratedResourceClient.GenresObject.Tools;

namespace GeneratedResourceClient.GenresObject.Types;

public class GenResObjectCollection : Dictionary<string, IDictionary<string, IGenResObject>>
{
    public void Add(IEnumerable<IGenResObject> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public IGenResObject Add(IGenResObject item)
    {
        var key = item._reference;
        var type = item.type;

        if (!ContainsKey(type))
        {
            this[type] = new Dictionary<string, IGenResObject>();
        }

        if (this[type].ContainsKey(key))
        {
            var exist = this[type][key];
            var target = (IGenResObject)ObjectMerger.MergeObjects(exist, item);
            this[type][key] = target;
        }
        else
        {
            this[type].Add(key, item);
        }

        return this[type][key];
    }
}