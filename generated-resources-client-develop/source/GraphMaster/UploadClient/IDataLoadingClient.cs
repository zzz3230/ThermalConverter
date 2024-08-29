using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Models;
using Nntc.Messaging.Kafka;

namespace GeneratedResourceClient.GraphMaster.UploadClient
{
    public interface IDataLoadingClient
    {
        LoadStatus LoadAll(
            ObjectGraph graph,
            Guid traceId,
            Guid? modelId,
            TypeInfoResolver? typeInfoResolver = null,
            string operation = "add",
            List<MessageChainEntry>? chainEntries = null,
            List<ConnectionModel>? connections = null,
            ModelAction? modelAction = null,
            List<DeleteAction>? deleteActions = null);

        public LoadStatus LoadAll(
            IDictionary<string, List<IDictionary<string, object>>> items,
            Guid traceId,
            Guid? modelId,
            string operation = "add",
            List<ConnectionModel>? connections = null,
            List<MessageChainEntry>? chainEntries = null,
            string? topic = null,
            ModelAction? modelAction = null,
            List<DeleteAction>? deleteActions = null);

        public LoadWithSplitStatus LoadWithSplit(
            ObjectGraph graph,
            Guid traceId,
            Guid? modelId,
            TypeInfoResolver? typeInfoResolver = null,
            string operation = "add",
            List<MessageChainEntry>? chainEntries = null,
            List<ConnectionModel>? connections = null,
            ModelAction? modelAction = null, 
            List<DeleteAction>? deleteActions = null);
    }
}
