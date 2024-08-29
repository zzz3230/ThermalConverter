using System.Collections;

namespace GeneratedResourceClient.GraphMaster.Tools;

public class GeneratedResourcesCollection : Dictionary<string, List<IDictionary<string, object>>>, IGeneratedResourcesCollection
{
    /// <summary>
    /// Добавить объекты к сообщению Генерируемых ресурсов
    /// </summary>
    /// <param name="src">Объекты</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Add(IGeneratedResourcesCollection src)
    {
        foreach (var item in src)
        {
            if (!TryGetValue(item.Key, out var resList))
            {
                this[item.Key] = resList = new List<IDictionary<string, object>>();
            }

            resList.AddRange(item.Value);
        }
    }
    public GeneratedResourcesCollection()
    { }

    public GeneratedResourcesCollection(IDictionary<string, IList<IDictionary<string, object>>> source)
    {
        foreach (var type in source)
        {
            this[type.Key] = type.Value.ToList();
        }
    }
}

public interface IGeneratedResourcesCollection : IDictionary<string, List<IDictionary<string, object>>>
{
    public void Add(IGeneratedResourcesCollection src);
}
