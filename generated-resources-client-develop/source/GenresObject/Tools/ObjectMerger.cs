using GeneratedResourceClient.Graph;
using LanguageExt;
using System.Reflection;
using System.Runtime.Serialization;

namespace GeneratedResourceClient.GenresObject.Tools;

public class ObjectMerger
{
    private static readonly DictionaryCache<Type, PropertyInfo[]> PropertyCache = new(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    private static readonly DictionaryCache<Type, FieldInfo[]> FieldCache = new(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance));
    private static readonly DictionaryCache<PropertyInfo, Option<Action<object, object>>> PropertySetterCache = new(info => info.GetPropertySetter());

    public static object MergeObjects(object oldObject, object newObject)
    {
        if (oldObject == null || newObject == null)
        {
            throw new ArgumentNullException("Оба объекта не должны быть null");
        }

        var objectType = oldObject.GetType();

        var mergedObject = FormatterServices.GetUninitializedObject(objectType);

        var properties = PropertyCache.Get(objectType);
        var fields = FieldCache.Get(objectType);

        foreach (var property in properties)
        {
            var newValue = property.GetValue(newObject);
            var setterOption = PropertySetterCache.Get(property);
            var oldValue = property.GetValue(oldObject);
            object target;

            if (newValue != null && oldValue != null && (property.Name is "Id" or "id" || property.Name.EndsWith("Id")))
            {
                target = oldValue;
            }
            else
            {
                target = (newValue ?? oldValue)!;
            }
            
            setterOption.IfSome(setter => setter(mergedObject, target));
        }

        foreach (var field in fields)
        {
            var newValue = field.GetValue(newObject);

            field.SetValue(mergedObject, newValue ?? field.GetValue(oldObject));
        }

        return mergedObject!;
    }
}