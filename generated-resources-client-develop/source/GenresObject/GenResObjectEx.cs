namespace GeneratedResourceClient.GenresObject;

public static class GenResObjectEx
{
    public static IDictionary<string, List<IGenResObject>> GroupByType(this IEnumerable<IEnumerable<IGenResObject>> source)
    {
        return source.SelectMany(x => x).GroupByType();
    }

    public static IDictionary<string, List<IGenResObject>> GroupByType(this IEnumerable<IGenResObject> source)
    {
        return source.GroupBy(x => x.type)
            .ToDictionary(x => x.Key, x => x.ToList());
    }
}
