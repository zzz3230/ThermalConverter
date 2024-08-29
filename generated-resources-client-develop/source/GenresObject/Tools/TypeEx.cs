using System.Reflection;
using LanguageExt;

namespace GeneratedResourceClient.GenresObject.Tools;

public static class TypeEx
{
    public static Option<Action<object, object>> GetPropertySetter(this PropertyInfo prop)
    {
        var setter = prop.GetSetMethod(nonPublic: true);

        if (setter != null)
        {
            return Option<Action<object, object>>.Some((obj, value) => setter.Invoke(obj, new[] { value }));
        }
        else
        {
            var backingField = prop.DeclaringType!.GetField($"<{prop.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

            if (backingField == null)
            {
                return Option<Action<object, object>>.None;
            }

            return Option<Action<object, object>>.Some((obj, value) => backingField.SetValue(obj, value));
        }
    }
}