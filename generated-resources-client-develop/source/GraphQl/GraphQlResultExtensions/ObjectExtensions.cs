using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratedResourceClient.GraphQl.GraphQlResultExtensions
{
    public static class ObjectExtensions
    {
        public static object? GetItem(this object obj, Path path, bool checkAmbiguity = false)
        {
            var items = obj.GetItems(path).ToArray();
            if (checkAmbiguity && items.Length > 1)
            {
                throw new InvalidOperationException($"Неоднозначное определение элемента {path}");
            }

            return items.FirstOrDefault();
        }

        public static IEnumerable<object> GetItems(this object obj, Path path)
        {
            var res = new List<object>();
            var pathSegments = path.GetSegments();
            if (pathSegments.Count == 0)
            {
                res.Add(obj);
                return res;
            }

            var currentSegment = pathSegments[0];

            var objectAsDict = obj as IDictionary<string, object>;
            if (objectAsDict != null)
            {
                if (currentSegment.IsAnyPath)
                {
                    var subPath = path.CreateSubPath(1);
                    var localObjects1 = objectAsDict.Values;
                    foreach (var subItem in localObjects1)
                    {
                        res.AddRange(subItem.GetItems(path));
                        res.AddRange(GetItems(subItem, subPath));
                    }
                }
                else
                if (currentSegment.IsAnyKey)
                {
                    var localObjects = objectAsDict.Values;
                    var subPath = path.CreateSubPath(1);
                    foreach (var subItem in localObjects)
                    {
                        res.AddRange(GetItems(subItem, subPath));
                    }
                }
                else
                {
                    if (currentSegment.IsKey && objectAsDict.TryGetValue(currentSegment.Key!, out var subObj))
                    {
                        var subPath = path.CreateSubPath(1);
                        var localObjects = GetItems(subObj, subPath);
                        res.AddRange(localObjects);
                    }
                }
            }
            else
            {
                var objAsIEnumerable = obj as IList<object>;
                if (objAsIEnumerable != null)
                {
                    if (currentSegment.IsIndex)
                    {
                        if (objAsIEnumerable.Count > currentSegment.Index!.Value)
                        {
                            var subPath = path.CreateSubPath(1);
                            var subItem = objAsIEnumerable[currentSegment.Index.Value];
                            var localObjects = GetItems(subItem, subPath);
                            res.AddRange(localObjects);
                        }
                    }
                    else
                    {
                        foreach (var subItem in objAsIEnumerable)
                        {
                            var localObjects = subItem.GetItems(path);
                            res.AddRange(localObjects);
                        }
                    }
                }
            }

            return res;
        }
    }
}
