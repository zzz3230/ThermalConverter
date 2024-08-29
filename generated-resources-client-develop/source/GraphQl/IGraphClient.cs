namespace GeneratedResourceClient.GraphQl;

public interface IGraphClient
{
    Task<T?> Get<T>(string query);

    /// <summary>
    /// Отправляет запрос в graphQL
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <typeparam name="X">Тип параметров</typeparam>
    /// <param name="query">Запрос в GraphQL</param>
    /// <param name="parameters">параметры запроса</param>
    /// <returns>Десериализованый результат выполнения запроса</returns>
    /// <exception cref="Exception">Ошибка при выполнении запроса</exception>
    Task<T?> Get<X, T>(string query, X parameters);
}