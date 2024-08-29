using System.Globalization;
using GeneratedResourceClient.GraphQl;
using Newtonsoft.Json;

namespace GeneratedResourceClient.Export
{
#pragma warning disable IDE1006 // Стили именования
    record NodeCollection<T>(List<T> Nodes, PageInfo PageInfo);

    class ModelObjectsItem<T> : Dictionary<string, Dictionary<string, NodeCollection<T>>>
    {

    }

    class DataItem<T> : Dictionary<string, NodeCollection<T>>
    {

    }
    public record Scheme(string Name);
    public record Model(string Name, Guid Id);
    public record Element(Guid Id, List<Property> Properties, Model? Model, Scheme Scheme);
    public record Property(Guid schemeId, Guid elementId, string Time, string Code, string StringValue, double? DoubleValue, IEnumerable<IDictionary<string, object>>? timeSeries);

    public static class ModelEx
    {
        public static object GetValue(this Property property)
        {
            if (property.timeSeries != null)
                return property.timeSeries;

            if (property.DoubleValue != null) return Math.Round((double)property.DoubleValue, 5);

            if (DateTime.TryParseExact(property.StringValue, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            return property.StringValue;
        }
    }

    public class GeneratedResourceExporter
    {
        private readonly IGraphClient _graphQlApi;

        private const int RequestElementsCount = 2000;

        private record Data<T>(NodeCollection<T> objects, Guid schemeId);
        private record RequestParams(int first, string? afterCursor, Guid modelId);
        public GeneratedResourceExporter(IGraphClient graphQlApi)
        {
            _graphQlApi = graphQlApi;
        }

        /// <summary>
        /// Выполняет запрос для выгрузки коллекции
        /// </summary>
        /// <param name="func">Функция запроса</param>
        /// <returns></returns>
        private async Task<List<T>> GetElementsCollection<T>(Func<string, Task<DataItem<T>>> func)
        {
            var respList = new List<T>();
            string? cursor = default;
            while (true)
            {
                var res = await func(cursor);
                var nodesCollection = res?.FirstOrDefault().Value;
                var newNodes = nodesCollection?.Nodes;

                if (newNodes is null || !newNodes.Any())
                    break;

                respList.AddRange(newNodes);

                cursor = nodesCollection?.PageInfo?.EndCursor;
            }

            return respList;
        }

        /// <summary>
        /// Выполняет запрос для выгрузки коллекции
        /// </summary>
        /// <param name="func">Функция запроса</param>
        /// <returns></returns>
        private async Task<List<T>> GetModelObjectElementsCollection<T>(Func<string, Task<ModelObjectsItem<T>>> func)
        {
            var respList = new List<T>();
            string? cursor = default;
            while (true)
            {
                var res = await func(cursor);
                var nodesCollection = res?.FirstOrDefault().Value?.Values.FirstOrDefault();
                var newNodes = nodesCollection?.Nodes;

                if (newNodes is null || !newNodes.Any())
                    break;

                respList.AddRange(newNodes);

                cursor = nodesCollection?.PageInfo?.EndCursor;
            }

            return respList;
        }

        /// <summary>
        /// Возвращает обьекты
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IDictionary<string, object>>> GetElements(string query, Guid modelId)
        {
            var rawItems = await GetElementsCollection(cursor => _graphQlApi.Get<RequestParams, DataItem<IDictionary<string, object>>>(query, new RequestParams(RequestElementsCount, cursor, modelId))!);
            return ParseItemsFromLoadedElements(rawItems);
        }

        /// <summary>
        /// Возвращает обьекты
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IDictionary<string, object>>> GetMasterDataObjectsElements(string query, Guid modelId)
        {
            var rawItems = await GetModelObjectElementsCollection(cursor => _graphQlApi.Get<RequestParams, ModelObjectsItem<IDictionary<string, object>>>(query, new RequestParams(RequestElementsCount, cursor, modelId))!);
            return ParseItemsFromLoadedElements(rawItems);
        }

        private async Task<IEnumerable<IDictionary<string, object>>> GetElements(string query, IDictionary<string, object> parameters)
        {
            var rawItems = await GetElementsCollection(cursor =>
            {
                var p = new Dictionary<string, object>()
                {
                    { "first", RequestElementsCount },
                    { "afterCursor", cursor },
                };

                foreach (var userParameter in parameters)
                {
                    p.Add(userParameter.Key, userParameter.Value);
                }

                return _graphQlApi.Get<IDictionary<string, object>, DataItem<IDictionary<string, object>>>(query, p)!;
            });

            return ParseItemsFromLoadedElements(rawItems);
        }

        private async Task<IEnumerable<IDictionary<string, object>>> GetMasterDataObjectsElements(string query, IDictionary<string, object> parameters)
        {
            var rawItems = await GetModelObjectElementsCollection(cursor =>
            {
                var p = new Dictionary<string, object>()
                {
                    { "first", RequestElementsCount },
                    { "afterCursor", cursor },
                };

                foreach (var userParameter in parameters)
                {
                    p.Add(userParameter.Key, userParameter.Value);
                }

                return _graphQlApi.Get<IDictionary<string, object>, ModelObjectsItem<IDictionary<string, object>>>(query, p)!;
            });

            return ParseItemsFromLoadedElements(rawItems);
        }
        /// <summary>
        /// Получает и десериализует элементы в нужный тип
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        [Obsolete("Модельные объекты теперь располагаются в поле modelObjects, мастер-объекты в masterObjects. Использовать метод GetMasterDataObjectsElements и его перегрузки. Старый метод оставлен для совместимости.")]
        public async Task<IEnumerable<T>> GetElements<T>(string query, Guid modelId)
        {
            var items = await GetElements(query, modelId);
            return items.Select(JsonConvert.SerializeObject).Select(JsonConvert.DeserializeObject<T>)!;
        }

        public async Task<IEnumerable<T>> GetMasterDataObjectsElements<T>(string query, Guid modelId)
        {
            var items = await GetMasterDataObjectsElements(query, modelId);
            return items.Select(JsonConvert.SerializeObject).Select(JsonConvert.DeserializeObject<T>)!;
        }

        [Obsolete("Модельные объекты теперь располагаются в поле modelObjects, мастер-объекты в masterObjects. Использовать метод GetMasterDataObjectsElements и его перегрузки. Старый метод оставлен для совместимости.")]
        public async Task<IEnumerable<T>> GetElements<T>(string query, IDictionary<string, object> parameters)
        {
            var items = await GetElements(query, parameters);
            return items.Select(JsonConvert.SerializeObject).Select(JsonConvert.DeserializeObject<T>)!;
        }

        public async Task<IEnumerable<T>> GetMasterDataObjectsElements<T>(string query, IDictionary<string, object> parameters)
        {
            var items = await GetMasterDataObjectsElements(query, parameters);
            return items.Select(JsonConvert.SerializeObject).Select(JsonConvert.DeserializeObject<T>)!;
        }

        static IEnumerable<IDictionary<string, object>> ParseItemsFromLoadedElements(List<IDictionary<string, object>> loaded) => loaded.Select(ToFlatDictionary);
        static IDictionary<string, object> ToFlatDictionary(IDictionary<string, object> source) => ToFlat(source).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        static IEnumerable<KeyValuePair<string, object>> ToFlat(IDictionary<string, object> source)
        {
            var type = "";
            foreach (var kvp in source)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Key is "type" && kvp.Value is IDictionary<string, object> dict)
                    {
                        type = dict.Values.First().ToString();
                        yield return new KeyValuePair<string, object>(kvp.Key, type);
                    }

                    else if (kvp.Value is IDictionary<string, object> child)
                    {
                        foreach (var flat in ToFlat(child))
                        {
                            if (flat.Key == "globalId" && !string.Equals(kvp.Key, type, StringComparison.CurrentCultureIgnoreCase))
                            {
                                yield return new KeyValuePair<string, object>(kvp.Key + "Id", flat.Value);
                            }
                            else
                            {
                                yield return flat;
                            }
                        }
                    }
                    else
                    {
                        yield return kvp;
                    }
                }
            }
        }
    }
}