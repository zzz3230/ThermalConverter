using CSEx.Json.Extensions;
using CSharpCustomExtensions.Collections.Dictionary;
using CSharpCustomExtensions.Flow;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Preprocessor;
using Microsoft.Extensions.Logging;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using Nntc.CSEx.Types.Collections.KeyValue;
using Nntc.ObjectModel;

namespace GeneratedResourceClient.GraphMaster.Validation;

/// <summary>
/// Валидатор обьектов на основе схемы OPM
/// </summary>
public interface IOpmValidator
{
    IEnumerable<string> Validate(Metadata metadata, IDictionary<string, List<IDictionary<string, object>>> items);
}

/// <summary>
/// Валидатор обьектов на основе схемы OPM
/// </summary>
public class OpmValidator : IOpmValidator
{
    private readonly ILogger<IOpmValidator> _logger;
    static OpmValidator()
    {
        JsonUtils.SerializationSettings.Converters.Add(new ReferenceJsonConvertor());
    }
    public OpmValidator(ILogger<IOpmValidator> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> Validate(Metadata metadata, IDictionary<string, List<IDictionary<string, object>>> items)
    {
        return ValidateFull(metadata, items).Distinct();
    }

    private IEnumerable<string> ValidateFull(Metadata metadata, IDictionary<string, List<IDictionary<string, object>>> items)
    {
        var types = metadata.Types.ToDictionary(x => x.Name, x => x);
        var objectTypeProps = types["Object"].Properties;

        foreach (var group in items)
        {
            if (group.Key is "object_relations")
                continue;

            if (types.TryGetValue(group.Key, out var type))
            {
                _logger.LogInformation($"Validate {group.Key}");

                if (group.Key is "Process")
                {

                    foreach (var processError in ValidateFull(metadata, group.Value.GroupBy(x => x.GetValue("ProcessType")).ToDictionary(x => x.Key.ToString()!, x => x.ToList())))
                    {
                        yield return processError;
                    }

                    continue;
                }

                var props = new LowerCaseDictionary<Property>();

                foreach (var property in type.WithBaseAndSubTypes().Properties.Concat(objectTypeProps).DistinctBy(x => x.Name))
                {
                    props.Add(property.Name, property);
                }

                foreach (var relationship in type.WithBaseAndSubTypes().RelationshipSet.All)
                {
                    var name = relationship.Target.NavigationName + "Id";
                    if (!props.ContainsKey(name))
                        props.Add(name, new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(Guid)) } });
                }

                foreach (var relationship in type.IncludesSubtypes.SelectMany(st => st.RelationshipSet.All))
                {
                    var name = relationship.Target.NavigationName + "Id";
                    if (!props.ContainsKey(name))
                        props.Add(name, new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(Guid)) } });
                }

                if (!props.ContainsKey("InfluencedObjectId"))
                    props.Add("InfluencedObjectId", new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(Guid)) } });

                if (!props.ContainsKey("id"))
                    props.Add("id", new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(Guid)) } });

                if (!props.ContainsKey("type"))
                    props.Add("type", new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(string)) } });

                if (!props.ContainsKey("needsMatch"))
                    props.Add("needsMatch", new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(bool?)) } });

                if (!props.ContainsKey("NeedsMatch"))
                    props.Add("NeedsMatch", new Property() { TypeInfo = new PropertyTypeInfo() { Schema = JsonSchema.FromType(typeof(bool?)) } });

                var settings = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver(), ReferenceLoopHandling = ReferenceLoopHandling.Serialize };

                foreach (var item in group.Value)
                {
                    foreach (var prop in item)
                    {
                        if (prop.Key is "Geometry")
                            continue;

                        if (props.TryGetValue(prop.Key, out var properScheme))
                        {
                            string json;

                            if (prop.Value is IEnumerable<object> collection && properScheme.TypeInfo.Name is not "NamedTimeSeries")
                            {
                                json = collection.Take(2).Pipe(x => JsonUtils.Serialize(x));
                            }
                            else
                            {
                                json = JsonUtils.Serialize(prop.Value);
                            }

                            var errors = properScheme.TypeInfo.Schema.Validate(JToken.Parse(json));

                            if (errors.Any())
                            {
                                foreach (var error in errors)
                                {
                                    yield return $"Type {group.Key}, property {prop.Key} --error: {error}, schema: type: {error.Schema.Type}, format: {error.Schema.Format}";
                                }
                            }
                        }
                        else
                        {
                            yield return $"Type {group.Key} no contains property {prop.Key}";
                        }
                    }
                }
            }
            else
            {
                yield return $"Type {group.Key} no contains in Metadata types";
            }
        }
    }
}
