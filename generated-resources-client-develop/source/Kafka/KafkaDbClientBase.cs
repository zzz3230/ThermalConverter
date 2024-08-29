using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Nntc.Authentication.JwtForwarding.Sources;
using Nntc.Kafka.AdvancedBuilders;
using Nntc.Messaging.Kafka;

namespace GeneratedResourceClient.Kafka
{
    /// <summary>
    /// Базовый клиент для отправки сообщений в Кафку (ГР)
    /// </summary>
    public abstract class KafkaDbClientBase
    {
        /// <summary>
        /// Топик в который кидаем сообщения
        /// </summary>
        protected readonly string Topic;

        /// <summary>
        /// Конфиг продюсера кафки
        /// </summary>
        private readonly ProducerConfig _config;
        protected readonly ILogger _logger;
        /// <summary>
        /// Token Source чтобы прокинуть JWT в кафку
        /// </summary>
        private readonly IJwtTokenSource _tokenSource;

        public KafkaDbClientBase(string topic, ProducerConfig config, ILogger logger, IJwtTokenSource tokenSource)
        {
            Topic = topic;
            _config = config;
            _logger = logger;
            _tokenSource = tokenSource;
        }

        /// <summary>
        /// Отправляет сообщение в Кафку.
        /// </summary>
        /// <param name="traceId">Идентификатор трассировки.</param>
        /// <param name="strData">Строковое представление данных для отправки.</param>
        /// <param name="topic">Топик в который нужно отправить сообщение. Если не указан, то используется дефолтный топик из конструктора.</param>
        /// <param name="chainEntries">Список цепочки отправки (для логирования).</param>
        /// <param name="operation">Операция для записи в цепочку отправки. По умолчанию "add".</param>
        /// <returns>Результат отправки.</returns>
        protected async Task<DeliveryResult<string, string>> SendMessage(Guid messageId, Guid traceId, string strData, string topic, List<MessageChainEntry> chainEntries, string operation)
        {
            chainEntries.Add(MessageChainEntry.Create(traceId, _config.ClientId, operation));

            _logger.LogDebug($"Send message to: {topic}");

            //await CheckAccessToken(_tokenSource);

            var producer = new AdvancedProducerBuilder<string, string>(_config, _tokenSource).Build();

            var headers = new MessageHeaders(messageId, traceId, chainEntries);

            var message = new Message<string, string>
            {
                Key = _config.ClientId,
                Value = strData,
                Headers = headers.ToKafkaHeaders()
            };

            var produceStatus = await producer.ProduceAsync(topic, message);

            _logger.LogDebug($"Message send, status {produceStatus.Status}");

            return produceStatus;
        }

        /// <summary>
        /// Проверяем наличие AccessToken из IJwtTokenSource
        /// </summary>
        /// <param name="tokenSource">Обьект IJwtTokenSource</param>
        /// <returns>Завершается успешно если удалось получить AccessToken</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private async Task CheckAccessToken(IJwtTokenSource tokenSource)
        {
            var _context = await _tokenSource.GetSourceContextAsync();

            if (_context?.AccessToken == null)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenSource), "Falied to get AccessToken from IJwnTokenSource");
            }
        }

        /// <summary>
        /// Отправляет сообщение в Кафку и возвращает статус отправки.
        /// </summary>
        /// <param name="traceId">Идентификатор трассировки.</param>
        /// <param name="strData">Строковое представление данных для отправки.</param>
        /// <param name="chainEntries">Список цепочки отправки (для логирования).</param>
        /// <param name="operation">Операция для записи в цепочку отправки. По умолчанию "add".</param>
        /// <param name="topic">Топик в который нужно отправить сообщение. Если не указан, то используется дефолтный топик из конструктора.</param>
        /// <returns>Результат отправки и список строковых представлений статусов отправки.</returns>
        protected bool Send(Guid messageId, Guid traceId, out List<string> messages, string strData, List<MessageChainEntry>? chainEntries, string operation = "add", string? topic = null)
        {
            var (status, sendMessages) = SendAsync(messageId, traceId, strData, chainEntries, operation, topic).Result;
            messages = sendMessages;
            return status;
        }

        /// <summary>
        /// Асинхронно отправляет сообщение в Кафку и возвращает статус отправки.
        /// </summary>
        /// <param name="traceId">Идентификатор трассировки.</param>
        /// <param name="strData">Строковое представление данных для отправки.</param>
        /// <param name="chainEntries">Список цепочки отправки (для логирования).</param>
        /// <param name="operation">Операция для записи в цепочку отправки. По умолчанию "add".</param>
        /// <param name="topic">Топик в который нужно отправить сообщение. Если не указан, то используется дефолтный топик из конструктора.</param>
        /// <returns>Результат отправки и список строковых представлений статусов отправки.</returns>
        protected async Task<(bool Status, List<string> Messages)> SendAsync(Guid messageId, Guid traceId, string strData, List<MessageChainEntry>? chainEntries, string operation = "add", string? topic = null)
        {
            var produceStatus = await SendMessage(messageId, traceId, strData, topic ?? Topic, chainEntries ?? new List<MessageChainEntry>(), operation);

            var status = produceStatus.Status switch
            {
                PersistenceStatus.PossiblyPersisted => "FAIL",
                PersistenceStatus.NotPersisted => "FAIL",
                PersistenceStatus.Persisted => "OK",
                _ => throw new ArgumentOutOfRangeException()
            };

            var messages = new List<string> { status };

            _logger.LogInformation($"Message {messageId} (trace {traceId}) send with offset {produceStatus.Offset}");

            _logger.LogInformation("Objects was loaded to Kafka with status {status}",
                new
                {
                    kafkaLoadStatus = produceStatus.Status,
                    kafkaLoadOffset = produceStatus.Offset,
                });

            return (produceStatus.Status == PersistenceStatus.Persisted, messages);
        }
    }
}
