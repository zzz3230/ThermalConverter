using System.Net;
using System.Text;
using CSEx.Json.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace GeneratedResourceClient.GraphQl
{
    internal record Response<T>(T Data, List<object> Errors);

    public record PageInfo(string StartCursor, string EndCursor);


    public class GraphClient : IGraphClient
    {
        private readonly ILogger<GraphClient> _logger;
        private readonly HttpClient _client;
        private readonly AsyncRetryPolicy _retryPolicy;

        public GraphClient(ILogger<GraphClient> logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
            _client.Timeout = TimeSpan.FromMinutes(20);

            // Создаем политику повтора с экспоненциальной задержкой
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(6, // Максимальное количество попыток (включая первоначальный запрос)
                    retryAttempt =>
                    {
                        var time = TimeSpan.FromMinutes(Math.Pow(2, retryAttempt));
                        if (time.TotalMinutes > 3)
                            return TimeSpan.FromMinutes(3);
                        return time;
                    },
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogError($"Request failed on retry {retryCount}. Retrying in {timeSpan.TotalSeconds} seconds.");
                    });
        }

        public async Task<HttpResponseMessage> SendRequestAsync(Func<HttpRequestMessage> requestGenerator)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = requestGenerator();
                _logger.LogInformation($"Send request to {request.RequestUri}");

                var response = await _client.SendAsync(request);
                try
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.BadRequest || ex.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        var msg = await response.Content.ReadAsStringAsync();
                        throw new Exception(msg);
                    }
                    
                    _logger.LogError($"Request failed, will retry. status code : {ex.StatusCode}");
                    throw;
                }
            });
        }

        public async Task<T?> Get<T>(string query) => await Get<object?, T>(query, default);

        /// <summary>
        /// Отправляет запрос в graphQL
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения</typeparam>
        /// <typeparam name="X">Тип параметров</typeparam>
        /// <param name="query">Запрос в GraphQL</param>
        /// <param name="parameters">параметры запроса</param>
        /// <returns>Десериализованый результат выполнения запроса</returns>
        /// <exception cref="Exception">Ошибка при выполнении запроса</exception>
        public async Task<T?> Get<X, T>(string query, X parameters)
        {
            HttpRequestMessage RequestMessageGenerator(string s)
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "graphql/")
                {
                    Content = new StringContent(s, Encoding.UTF8, "application/json"),
                };

                httpRequestMessage.Headers.Add("Accept-Encoding", "deflate");
                return httpRequestMessage;
            }

            

            var body = new
            {
                query,
                variables = parameters
            };

            var stringBody = JsonConvert.SerializeObject(body);

            _logger.LogDebug($"GraphQL {stringBody}");

            var resp = await SendRequestAsync(() => RequestMessageGenerator(stringBody));

            var content = await resp.ReadContentAsync<Response<T>>();

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                if (content?.Errors?.Any() ?? false)
                    throw new Exception($"Fail get data from graphQL : {JsonConvert.SerializeObject(content.Errors)}");
                _logger.LogDebug($"GraphQL query completed");
                return content!.Data;
            }
            else
            {
                throw new Exception($"{resp.StatusCode}, Http errors on sending graphql query {query}, errors:{JsonUtils.Serialize(content)}");
            }
        }
    }
    public static class HttpResponceMessageExtension
    {
        public static async Task<string> ReadContentAsync(this HttpResponseMessage message)
        {
            var buffer = await message.Content.ReadAsByteArrayAsync();
            var byteArray = buffer.ToArray();
            return Encoding.Default.GetString(byteArray, 0, byteArray.Length);
        }

        public static async Task<T> ReadContentAsync<T>(this HttpResponseMessage message) => JsonConvert.DeserializeObject<T>(await message.ReadContentAsync(), new JsonSerializerSettings()
        {
            Converters = new List<JsonConverter>()
            {
                new JsonDictionaryConverter()
            }
        });
    }
}
