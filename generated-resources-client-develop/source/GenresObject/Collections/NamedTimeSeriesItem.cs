namespace GeneratedResourceClient.GenresObject.Collections;

public class NamedTimeSeriesItem<T>
{
    public NamedTimeSeriesItem(string? key, IEnumerable<TimeSeriesValue<T>>? values, bool isInterval)
    {
        Key = key;
        Values = values?.ToList() ?? new List<TimeSeriesValue<T>>();
        this.isInterval = isInterval;
    }

    public bool isInterval { get; }

    public string? Key { get; }

    public IEnumerable<TimeSeriesValue<T>>? Values { get; }

    public override string ToString() => Key!;
}

public class NamedTimeSeriesItem : NamedTimeSeriesItem<double>
{
    public NamedTimeSeriesItem(string? key, IEnumerable<TimeSeriesValue<double>>? values, bool isInterval) : base(key, values, isInterval)
    {
    }
}


