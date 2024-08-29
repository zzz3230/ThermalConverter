using System.Collections;
using Confluent.Kafka;
using GeneratedResourceClient.GraphMaster.UploadClient;
using GeneratedResourceClient.GraphMaster.Validation;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using Nntc.Authentication.JwtForwarding.Kafka;
using Nntc.Authentication.JwtForwarding.Kafka.JwtForwarding;
using Nntc.ObjectModel;
using Nntc.ObjectModel.Flags;
using Metadata = Nntc.ObjectModel.Metadata;

/// <summary>
/// Usage:<br/>
/// s = new(); <br/>
/// s.AddType();<br/>
/// ...<br/>
/// s.AddType();<br/>
/// s.SetupUploader(); <br/>
/// s.Upload();<br/>
/// ...<br/>
/// s.Upload();<br/>
/// </summary>
public class ReportSender
{
    private readonly ILogger<ReportSender> _logger;
    private GeneratedResourceKafkaUploader _uploader;
    private readonly ProducerConfig _config;
    private List<ObjectType> _types = [];

    public ReportSender(ILoggerFactory loggerFactory, string bootstrapServers)
    {
        _logger = loggerFactory.CreateLogger<ReportSender>();
        
        _config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "1DCDE728-224B-4E6C-B260-63581B2C7016"
        };
        
    }

    Property MakeProperty<T>(string name)
    {
        return new() { Name = name, TypeInfo = new() { Schema = JsonSchema.FromType<T>() } };
    }
    Property MakeProperty(string name)
    {
        return new() { Name = name, TypeInfo = new() { Schema = new() } };
    }

    public void AddType(string objectTypeName, List<string> customParams)
    {
        List<Property> properties = [];
        
        properties.AddRange(customParams.Select(MakeProperty));
        
        _types.Add(new ObjectType()
        {
            Name = objectTypeName, 
            Properties = properties, 
            IncludesSubtypes = [
                new ObjectType()
                {
                    Name = "element", 
                    Properties = [MakeProperty<string>("gatheringNetworkId")]
                }]
        });
    }
    
    public void SetupUploader(ILoggerFactory loggerFactory, string topic)
    {
        _types.Add(new ObjectType{Name = "Object"});
        
        var metadata = new Metadata()
        {
            Types = _types,
        };

        _uploader = new GeneratedResourceKafkaUploader(
            _config, 
            metadata, 
            topic, 
            new Logger<GeneratedResourceKafkaUploader>(loggerFactory), 
            new OpmValidator(loggerFactory.CreateLogger<OpmValidator>()), 
            new KafkaJwtTokenSource(new KafkaContextAccessor() {})
        );
    }

    public void Upload(IDictionary<string, List<IDictionary<string, object>>> objects)
    {
        _uploader.LoadAll(objects, Guid.NewGuid(), Guid.NewGuid());
    }
}