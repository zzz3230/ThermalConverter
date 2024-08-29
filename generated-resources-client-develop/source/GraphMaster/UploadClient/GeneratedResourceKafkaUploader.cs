using Confluent.Kafka;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Models;
using GeneratedResourceClient.GraphMaster.Preprocessor;
using GeneratedResourceClient.GraphMaster.Validation;
using GeneratedResourceClient.Kafka;
using Microsoft.Extensions.Logging;
using Nntc.Authentication.JwtForwarding.Sources;
using Nntc.Messaging.Kafka;
using CSEx.Json.Extensions;
using Nntc.ObjectModel;
using Metadata = Nntc.ObjectModel.Metadata;
using CSharpCustomExtensions.Collections.Dictionary;
using Newtonsoft.Json;
using CSharpCustomExtensions.Flow;

namespace GeneratedResourceClient.GraphMaster.UploadClient
{
    public class ConnectionModel
    {
        [JsonProperty("action")]
        public string Action { get; set; } = "add";

        public ConnectionModel(string sourceType, string navigationName, List<Relation> connections)
        {
            SourceType = sourceType;
            NavigationName = navigationName;
            Connections = connections;
        }

        [JsonProperty("sourceType")]
        public string SourceType { get; set; }

        [JsonProperty("navigationName")]
        public string NavigationName { get; set; }

        [JsonProperty("connections")]
        public List<Relation> Connections { get; set; }
    }

    public class Relation
    {
        public Relation(Guid from, Guid to)
        {
            From = from;
            To = to;
        }

        public Guid From { get; set; }
        public Guid To { get; set; }
    }
    public record LoadStatus(Guid MessageId, bool IsLoaded, bool HaveErrors, string? Message = null);
    public class LoadWithSplitStatus
    {

        public LoadWithSplitStatus(bool isLoaded, List<LoadStatus> statues)
        {
            IsLoaded = isLoaded;
            Statues = statues;
            MessageIds = statues.Select(s => s.MessageId).Where(m => m != Guid.Empty).ToList();
            var errors = statues.Where(s => !s.HaveErrors).ToList();
            HaveErrors = errors.Any();
            Message = string.Join("\n", errors);
        }

        public bool IsLoaded { get; }
        public List<LoadStatus> Statues { get; }
        public IEnumerable<Guid> MessageIds { get; }
        public bool HaveErrors { get; }
        public string Message { get; }
    }

    public class GeneratedResourceKafkaUploader : KafkaDbClientBase, IDataLoadingClient
    {
        private readonly Metadata _metadata;
        private readonly IOpmValidator _validator;
        private readonly GeneratedResourceUploadPreprocessor _generatedResourceUploadPreprocessor;
        private readonly Dictionary<string, ObjectType> _types;
        private readonly ObjectType _objectType;
        private readonly SizeSplitter _splitter;

        public GeneratedResourceKafkaUploader(ProducerConfig producerConfig, Metadata metadata, string kafkaTopic, ILogger<GeneratedResourceKafkaUploader> logger, IOpmValidator validator, IJwtTokenSource tokenSource) : base(kafkaTopic, producerConfig, logger, tokenSource)
        {
            _metadata = metadata;
            _validator = validator;
            _generatedResourceUploadPreprocessor = new GeneratedResourceUploadPreprocessor(metadata.Types.ToDictionary(x => x.Name));
            _types = metadata.Types.ToDictionary(x => x.Name);
            _objectType = _types["Object"];
            _splitter = new SizeSplitter(_types);
        }

        private IDictionary<string, List<IDictionary<string, object>>> RemoveUnRegisteredAttributes(IDictionary<string, List<IDictionary<string, object>>> objects)
        {
            //TODO Проверить
            foreach (var res in objects)
            {
                if (res.Key is "object_relations") continue;

                if (!_types.ContainsKey(res.Key)) continue;

                var type = _types[res.Key];

                if (type.Name is "Process")
                {
                    RemoveUnRegisteredAttributes(res.Value.GroupBy(x => x.GetValue("ProcessType").ToString()).ToDictionary(x => x.Key!, x => x.ToList()));
                    continue;
                }

                var props = type.WithBaseAndSubTypes().Combine(_objectType).Properties.Select(x => x.Name.ToLower()).ToHashSet();

                var relations = type.WithBaseAndSubTypes().Combine(_objectType).RelationshipSet.All.Concat(type.IncludesSubtypes.SelectMany(st => st.RelationshipSet.All)).ToList();

                foreach (var relation in relations)
                {
                    props.Add((relation.Target.NavigationName + "Id").ToLower());
                }

                res.Value.ForEach(x =>
                    x.RemoveAll(a =>
                        a.Key is "rowId"
                        || (!a.Key.Equals("Id", StringComparison.InvariantCultureIgnoreCase)
                            && a.Key is not ("type" or "needsMatch" or "NeedsMatch" or "NameShortRu" or "Geometry")
                            && !props!.Contains(a.Key.ToLower()))));
            }

            return objects;
        }

        public LoadWithSplitStatus LoadWithSplit(
            ObjectGraph graph,
            Guid traceId,
            Guid? modelId,
            TypeInfoResolver? typeInfoResolver = null,
            string operation = "add",
            List<MessageChainEntry>? chainEntries = null,
            List<ConnectionModel>? connections = null,
            ModelAction? modelAction = null,
            List<DeleteAction>? deleteActions = null
        )
        {
            var splitted = _splitter.Split(graph).ToList();

            var statues = splitted
                .Select(sc => LoadAll(sc, traceId, modelId, typeInfoResolver, operation, chainEntries, connections, modelAction, deleteActions))
                .ToList();

            return new LoadWithSplitStatus(statues.All(s => s.IsLoaded), statues);
        }

        public LoadStatus LoadAll(
            ObjectGraph graph,
            Guid traceId,
            Guid? modelId,
            TypeInfoResolver? typeInfoResolver = null,
            string operation = "add",
            List<MessageChainEntry>? chainEntries = null,
            List<ConnectionModel>? connections = null,
            ModelAction? modelAction = null,
            List<DeleteAction>? deleteActions = null
        )
        {
            if (!graph.Vertices.Any())
            {
                return new LoadStatus(Guid.Empty, true, false, "В сообщении не было ни одного обьекта");
            }

            var groups = graph.Copy().ToTypedCollection(typeInfoResolver ?? new TypeInfoResolver(_types, null!));

            var converted = groups.GeneratedResourcesCollection;

            var cleaned = RemoveUnRegisteredAttributes(converted);

            var graphConnections = groups.Relations.Select(rs => new ConnectionModel(rs.Key.From, rs.Key.To, rs.Value.Select(v => new Relation(v.From, v.To)).ToList())).ToList();

            if (connections != null)
            {
                graphConnections.AddRange(connections);
            }

            _logger.LogInformation($"Will validate {traceId}");
            var isRemove = cleaned.Remove("KeyValuePair");
            _logger.LogInformation($"Remove KVP status : {isRemove}");

            return LoadAll(cleaned, traceId, modelId, operation, graphConnections, chainEntries, modelAction: modelAction, deleteActions: deleteActions);
        }

        public LoadStatus LoadAll(
            IDictionary<string, List<IDictionary<string, object>>> items,
            Guid traceId,
            Guid? modelId,
            string operation = "add",
            List<ConnectionModel>? connections = null,
            List<MessageChainEntry>? chainEntries = null,
            string? topic = null,
            ModelAction? modelAction = null,
            List<DeleteAction>? deleteActions = null
        )
        {
            var errors = _validator.Validate(_metadata, items).ToList();

            if (errors.Any())
            {
                var error = string.Join(", ", errors);
                _logger.LogInformation($"errors : {error}");
                return new LoadStatus(Guid.Empty, false, true, error);
            }

            var models = _generatedResourceUploadPreprocessor.CreateReceiveModels(items);

            string strData;
            var messageId = Guid.NewGuid();

            if (modelId == null)
            {
                var error = "modelId is null";
                _logger.LogInformation($"errors : {error}");
                return new LoadStatus(Guid.Empty, false, true, error);
            }

            strData = new ModelMessageBody<ReciveModel>(messageId, modelId, modelAction, models, connections, deleteActions ?? new List<DeleteAction>()).Pipe(JsonUtils.Serialize);

            var status = Send(messageId, traceId, out var messages, strData, chainEntries, operation, topic);

            return new LoadStatus(messageId, status, false, string.Join(", ", messages));
        }
    }
}