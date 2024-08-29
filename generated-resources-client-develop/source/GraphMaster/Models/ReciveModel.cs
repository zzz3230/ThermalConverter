using CSharpCustomExtensions.Collections.Dictionary;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using GeneratedResourceClient.GraphMaster.UploadClient;

namespace GeneratedResourceClient.GraphMaster.Models
{
    public enum Group
    {
        Object, Process, Register
    }
    public class ReciveModel
    {
        public ReciveModel(string type, IDictionary<string, object> data, bool isRef = false, Geometry geometry = null, bool? needsMatch = null)
        {
            isReference = isRef;
            this.geometry = geometry;
            this.type = type;
            this.data = data;
            _ = int.TryParse(data.GetOrDefault("rowId", "0").ToString(), out var row);
            rowId = row - 1;

            this.needsMatch = needsMatch;

            data.Remove("needsMatch");
            this.data.Remove("rowId");
            this.data.Remove("type");
        }

        public bool? needsMatch { get; set; }
        public bool isReference { get; set; }
        public Geometry geometry { get; }
        public int rowId { get; set; }
        public string type { get; set; }
        public IDictionary<string, object> data { get; set; }
    }

    public class DeleteAction
    {
        public string Type { get; set; }
        public Guid[] Ids { get; set; }
    }

    public record ModelMessageBody<T>(Guid MessageId, Guid? ModelId, ModelAction? ModelAction, List<T> Data, List<ConnectionModel>? connections, List<DeleteAction>? Delete);
}
