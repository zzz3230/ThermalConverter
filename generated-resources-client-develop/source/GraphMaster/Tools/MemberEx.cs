using LanguageExt.ClassInstances;
using System.Reflection;

namespace GeneratedResourceClient.GraphMaster.Tools;

public static class MemberEx
{
    /// <summary>
    /// Имеет ли Тип, поле или свойство заданый аттрибут
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool HasAttribute<TAttribute>(this MemberInfo t) where TAttribute : Attribute => t.GetCustomAttribute<TAttribute>() != null;

    public static string GetDefaultTypeName(this Type t)
    {
        if (!t.IsGenericType)
        {
            return t.Name;
        }
        else
        {
            return t.Name.Substring(0, t.Name.IndexOf('`'));
        }
    }

    /// <summary>
    /// Имеет ли тип свойство (регистр значения не имеет)
    /// </summary>
    /// <param name="type">Тип</param>
    /// <param name="propertyName">Имя свойства</param>
    /// <returns></returns>
    public static bool HasProperty(this Type type, string propertyName) => GetPropertyIgnoreCase(type, propertyName) != null;
    public static PropertyInfo? GetPropertyIgnoreCase(this Type type, string propertyName)
    {
        var props = GetPropertiesIgnoreCase(type);

        return props.FirstOrDefault(item=> item.Name == propertyName);
    }

    public static IEnumerable<PropertyInfo> GetPropertiesIgnoreCase(this Type type)
    {
        var props = type.GetProperties(GetPropertyFlags);

        //Получаем только не скрытые поля классов, если полей с одинаковым именем больше 1
        var res = from item in props
        group item by item.Name into g
                  select g.OrderByDescending(t => t.DeclaringType == type).First();

        return res;
    }

    private const BindingFlags GetPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.IgnoreCase;
}