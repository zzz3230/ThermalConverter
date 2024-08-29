using CSharpCustomExtensions.Collections.Dictionary;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Validation;
using GeneratedResourceClient.Graph;

namespace GeneratedResourceClient.GraphMaster.Tools
{
    /// <summary>
    /// Атрибут для класса или свойства, указывающий, что это не отдельная сущность при формировании сообщений для Генерируемых ресурсов
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class NotGrEntityAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class GrEntityAttribute : Attribute { }

    /// <summary>
    /// Набор методов расширения для формирования сообщения для Генерируемых ресурсов на основе объектов
    /// Допускает работу с обычными и анонимными классами.
    /// Выделяет вложенные сущности, обрабатывает связи типа один ко многим.
    /// Чтобы анонимный класс рассматривался как отдельная сущность, необходимо, чтобы у объекта было свойство id
    /// Для получения адекватного имени типа объекта (например, для анонимных классов) нужно у объекта заполнять свойство type.
    /// Обычные классы можно уже создавать с правильным именем.
    /// Чтобы предотвратить обработку класса или свойства как отдельной сущности, можно использовать атрибут [NotGrEntity].
    /// Использование атрибутов необязательно.
    /// </summary>
    public class GeneratedResourcesConvertor
    {
        const string TypeFieldName = "type";
        const string IdFieldName = "id";

        private readonly IMemoryCache _propertyCache;
        private readonly IMemoryCache _embeddedCache;

        public GeneratedResourcesConvertor(IMemoryCache propertyCache, IMemoryCache embeddedCache)
        {
            _propertyCache = propertyCache;
            _embeddedCache = embeddedCache;
        }

        public GeneratedResourcesConvertor(IServiceProvider serviceProvider) : this(serviceProvider.GetService<IMemoryCache>()!, serviceProvider.GetService<IMemoryCache>()!)
        {
        }

        private Dictionary<string, PropertyInfo>? GetProperties(Type type) => _propertyCache.GetOrCreate(type, _ => type.GetPropertiesIgnoreCase().Where(property => !HasNotEntityAttribute(property)).ToDictionary(x => x.Name.ToLower()));
        private bool IsEmbeddedEntity(PropertyInfo pi) => _embeddedCache.GetOrCreate(pi.PropertyType.FullName!, _ => CheckEmbeddedEntity(pi));
        static bool IsPrimitive(PropertyInfo pi) => pi.PropertyType.IsPrimitive || pi.PropertyType == typeof(string);
        static bool HasNotEntityAttribute(MemberInfo member) => member?.HasAttribute<NotGrEntityAttribute>() ?? false;
        static bool HasEntityAttribute(MemberInfo member) => member?.HasAttribute<GrEntityAttribute>() ?? false;
        private bool IsHotChocolateOptionalEmpty(PropertyInfo prop, object? value) => GetProperties(prop.PropertyType)!["isempty"].GetValue(value) is true;
        private bool IsLXOptionEmpty(PropertyInfo prop, object? value) => GetProperties(prop.PropertyType)!["isnone"].GetValue(value) is true;

        private bool CheckEmbeddedEntity(PropertyInfo pi)
        {
            bool HasIdProperty(Type member) => GetProperties(member)?.ContainsKey(IdFieldName) ?? false;

            static bool TryGetGenericType(PropertyInfo pi, out Type genericType)
            {
                genericType = null!;

                if (!pi.PropertyType.IsGenericType || !pi.PropertyType.IsAssignableTo(typeof(IEnumerable)))
                    return false;

                genericType = pi.PropertyType.GenericTypeArguments[0];

                return true;
            }

            bool HaveSupportedGenericType(PropertyInfo pi) =>
                TryGetGenericType(pi, out var candidateEntityType) &&
                !HasNotEntityAttribute(candidateEntityType) &&
                HasIdProperty(candidateEntityType);

            return !IsPrimitive(pi) &&
                   !HasNotEntityAttribute(pi) &&
                   (HasEntityAttribute(pi) || HasEntityAttribute(pi.PropertyType) || HasIdProperty(pi.PropertyType) || HaveSupportedGenericType(pi));
        }

        public ObjectGraph Convert(object src, ObjectGraph? graph = null, bool removeNullOrEmptyValues = true) => Convert(src, removeNullOrEmptyValues, graph ?? new ObjectGraph(), null!, null);

        public IDictionary<string, object> ToDictionary(object item)
        {
            var type = item.GetType();

            var publicProperties = GetProperties(type)!;

            var id = publicProperties.GetValueOrDefault(IdFieldName)?.GetValue(item) ?? Guid.NewGuid();

            var typeKeyInner = GetTypeKey(item, publicProperties, type);

            var objMsg = new Dictionary<string, object>
            {
                { TypeFieldName, typeKeyInner },
                { IdFieldName, id }
            };

            foreach (var pair in publicProperties)
            {
                if (pair.Key == IdFieldName || pair.Key == TypeFieldName)
                    continue;

                var prop = pair.Value;

                var value = prop.GetValue(item);

                if (value is null || value is string str && string.IsNullOrWhiteSpace(str)) continue;

                if (!IsEmbeddedEntity(prop))
                {
                    objMsg[prop.Name] = value!;
                }
            }

            return objMsg;
        }
        string GetTypeKey(object src, IDictionary<string, PropertyInfo> propertyInfos, Type type)
        {
            string? name = null;

            var typeProperty = propertyInfos.GetOrDefault(TypeFieldName);

            if (typeProperty != null)
            {
                var propValue = typeProperty.GetValue(src, null);

                if (propValue is string value)
                {
                    name = value;
                }
            }


            if (string.IsNullOrEmpty(name))
            {
                name = type.GetDefaultTypeName();
            }

            return name;
        }

        public ObjectGraph Convert(object src, bool removeNullOrEmptyValues, ObjectGraph graph, IDictionary<string, object>? parent, string propertyName)
        {
            Guid ParseGuid(object id)
            {
                if (id is Guid giud) return giud;
                Guid.TryParse(id.ToString()!, out var gid);
                return gid;
            }

            var resGraph = ConvertLocal(src, removeNullOrEmptyValues, graph, parent, propertyName);

            Guid GetId(IDictionary<string, object> item) => ParseGuid(item["id"]);

            var vertexes = resGraph.Vertices.DistinctBy(GetId).ToDictionary(GetId);

            foreach (var vertex in resGraph.Vertices.DistinctBy(GetId))
            {
                var ids = vertex.Where(x => x.Key.EndsWith("Id") && x.Key != "id" && x.Value != null);

                foreach (var id in ids)
                {
                    try
                    {
                        var gId = ParseGuid(id.Value);
                        var found = vertexes.TryGetValue(gId, out var linked);

                        if (found)
                        {
                            vertex.Remove(id);
                            resGraph.AddEdge(vertex, linked!, linked!["type"].ToString()!);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Не удалось обработать id: [{id.Key} - {id.Value}], тип: {vertex.GetOrDefault("type")}", e);
                    }
                }
            }

            return resGraph;
        }
        /// <summary>
        /// Создать сообщение для Генерируемых ресурсов
        /// </summary>
        /// <param name="src">Объект - контейнер данных</param>
        /// <param name="removeNullOrEmptyValues">Удалять ли обьекты с пустыми значениями</param>
        /// <param name="parentId">Данные по ключу родителя (для вложенных объектов)</param>
        /// <returns>Сообщение для Генерируемых ресурсов</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ObjectGraph ConvertLocal(object src, bool removeNullOrEmptyValues, ObjectGraph graph, IDictionary<string, object>? parent, string propertyName)
        {
            Requires.NotNull(src, nameof(src));

            if (src is IEnumerable collection)
            {
                return ConvertLocal(collection, removeNullOrEmptyValues, graph, parent, propertyName);
            }

            var type = src.GetType();

            var publicProperties = GetProperties(type)!;

            var typeKeyInner = GetTypeKey(src, publicProperties, type);

            var id = publicProperties.GetValueOrDefault(IdFieldName)?.GetValue(src) ?? Guid.NewGuid();

            var objMsg = new Dictionary<string, object>
            {
                { TypeFieldName, typeKeyInner },
                { IdFieldName, id }
            };

            graph.AddVertex(objMsg);

            if (parent != null)
                graph.AddEdge(parent, objMsg, propertyName);

            foreach (var pair in publicProperties)
            {
                if (pair.Key == IdFieldName || pair.Key == TypeFieldName)
                    continue;

                var prop = pair.Value;

                var value = prop.GetValue(src);

                if (prop.PropertyType.Name.Contains("Optional"))
                {
                    if (IsHotChocolateOptionalEmpty(prop, value))
                    {
                        continue;
                    }
                    else
                    {
                        value = GetProperties(prop.PropertyType)!["value"].GetValue(value);
                    }
                }
                else if (prop.PropertyType.Name.Contains("Option"))
                {
                    if (IsLXOptionEmpty(prop, value))
                    {
                        continue;
                    }
                    else
                    {
                        value = GetProperties(prop.PropertyType)!["case"].GetValue(value);
                    }
                }
                if (removeNullOrEmptyValues && value is null || value is string str && string.IsNullOrWhiteSpace(str)) continue;

                if (value != null && IsEmbeddedEntity(prop)) // Дочерний обьект
                {
                    ConvertLocal(value, removeNullOrEmptyValues, graph, objMsg, prop.Name);
                }
                else // Примитивный тип
                {
                    objMsg[prop.Name] = value!;
                }
            }


            return graph;
        }

        /// <summary>
        /// Преобразовать список объектов в сообщение для Генерируемых ресурсов
        /// </summary>
        /// <param name="src">Объекты</param>
        /// <param name="removeNullOrEmptyValues">Удалять ли обьекты с пустыми значениями</param>
        /// <param name="parentId">Данные по ключу родителя (для вложенных объектов)</param>
        /// <returns>Сообщение для Генерируемых ресурсов</returns>
        public ObjectGraph ConvertLocal(IEnumerable src, bool removeNullOrEmptyValues, ObjectGraph graph, IDictionary<string, object> parent, string propertyName)
        {
            Requires.NotNull(src, nameof(src));

            foreach (var item in src)
            {
                ConvertLocal(item, removeNullOrEmptyValues, graph, parent, propertyName);
            }

            return graph;
        }
    }
}
