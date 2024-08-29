using System.Text.RegularExpressions;
using CSEx.Json.Extensions;
using CSharpCustomExtensions.Collections.Dictionary;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Nntc.ObjectModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GeneratedResourceClient.GraphMaster.Preprocessor;


public class GeneratedResourceUploadPreprocessor : IResourceUploadPreprocessor
{
    private readonly Dictionary<string, ObjectType> _typeCollection;

    public GeneratedResourceUploadPreprocessor(Dictionary<string, ObjectType> typeCollection)
    {
        _typeCollection = typeCollection;
    }

    public List<ReciveModel> CreateReceiveModels(IDictionary<string, List<IDictionary<string, object>>> items)
    {
        try
        {
            var models = new List<ReciveModel>();

            foreach (var group in items)
            {
                models.AddRange(group.Value.Select(x =>
                {
                    bool? needsMatch = null;
                    var nms = x.GetOrDefault("needsMatch") ?? x.GetOrDefault("NeedsMatch");

                    if (nms != null && bool.TryParse(nms.ToString(), out var nm))
                    {
                        needsMatch = nm;
                    }

                    return new ReciveModel(group.Key, Process(x), false, null, needsMatch);
                }));
            }

            return models;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Конверитирует обьекты для загрузки в ГР
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public IDictionary<string, object> Process(IDictionary<string, object> source)
    {
        void AddParameters(ObjectType subtype, Dictionary<string, IDictionary<string, object>> subItems1)
        {
            var subTypeProps = GetTypeProps(source, subtype).ToDictionary(x => x.Key, x => x.Value);
            source.RemoveAll(x => subTypeProps.ContainsKey(x.Key));

            if (subTypeProps.Any())
            {
                subItems1.Add(subtype.Name, subTypeProps);
            }
        }

        var id = source.GetOrDefault("id", null)?.ToString() ?? throw new Exception($"В обьекте отсутсвует id {JsonUtils.Serialize(source)}");
        var type = source.GetOrDefault("type", null)?.ToString() ?? throw new Exception($"В обьекте отсутсвует информация о типе {JsonUtils.Serialize(source)}");
        source.Remove("type");
        source.Remove("id");
        source.Remove("NeedsMatch");
        source.Remove("needsMatch");

        var typeData = _typeCollection.GetOrDefault(type, null) ?? throw new Exception($"Тип {type} отсутствует в OPM");
        var subItems = new Dictionary<string, IDictionary<string, object>>();

        foreach (var subtype in typeData.IncludesSubtypes)
        {
            AddParameters(subtype, subItems);
        }

        var names = source.Where(x => Regex.IsMatch(x.Key, "^Name(?:Full|Short)(?:Ru|En)$|Geometry$")).ToList();

        var res = new Dictionary<string, object>()
        {
            { "globalId", id }
        };

        foreach (var name in names)
        {
            res.Add(name.Key, name.Value);
            source.Remove(name.Key);
        }

        if (type is not null)
        {
            if (source.Any())
            {
                res.Add(type, source);
            }
        }

        foreach (var subItem in subItems)
        {
            res.Add(subItem.Key, subItem.Value);
        }
        return res;
    }

    private IEnumerable<KeyValuePair<string, object>> GetTypeProps(IDictionary<string, object> source, IObjectType type)
    {
        var props = type.Properties.Select(x => x.Name).Concat(type.RelationshipSet.All.Select(x => x.Target.NavigationName + "Id")).ToHashSet();

        foreach (var propertyValue in source.Where(p => props.Contains(p.Key)))
        {
            if(propertyValue.Value is Reference reference && reference.SubType != type.Name)
            {
                continue;
            }
            yield return propertyValue;
        }

        if (type.BaseType is not null)
        {
            foreach (var parentTypeProp in GetTypeProps(source, type.BaseType))
            {
                yield return parentTypeProp;
            }
        }
    }
}
