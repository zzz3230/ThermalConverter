
using System.Collections;

namespace GeneratedResourceClient.GenresObject.Collections;

public class NamedTimeSeriesCollection : IEnumerable<NamedTimeSeriesItem>
{
    private Dictionary<string, NamedTimeSeriesItem> _items = new Dictionary<string, NamedTimeSeriesItem>();
    public NamedTimeSeriesCollection() { }
    public NamedTimeSeriesCollection(IEnumerable<NamedTimeSeriesItem> values)
    {
        foreach (var val in values)
        {
            _items.Add(val.Key, val);
        }
    }

    public static implicit operator NamedTimeSeriesCollection(List<TimeSeriesValue> values)
    {
        var item = new NamedTimeSeriesItem("", values, false);
        return new NamedTimeSeriesCollection(new List<NamedTimeSeriesItem>() { item });
    }

    public NamedTimeSeriesItem this[string key] { get => _items[key]; set => _items[key.ToLower()] = value; }

    public void Add(NamedTimeSeriesItem item)
    {
        _items[item.Key] = item;
    }
    public int Count => _items.Count;

    public IEnumerator<NamedTimeSeriesItem> GetEnumerator()
    {
        return this._items.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool ContainsKey(string key)
    {
        return _items.ContainsKey(key);
    }
}



