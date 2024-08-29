using System.Text.Json.Serialization;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Tools;
using GeneratedResourceClient.GraphMaster.Validation;
using GeneratedResourceClient.Opm;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Nntc.ObjectModel;

namespace Tests;

public class ValidationTests
{
    [Test]
    public void TestValidation()
    {
        var gw = new GroupOfWells()
        {
            NameShortRu = "GrOfWells",
            //ParentGroupOfWellsId = Guid.NewGuid(),
            id = Guid.NewGuid(),
            Children = new List<GroupOfWells>()
            {
                new(){NameShortRu = "Test1"},
                new(){NameShortRu = "Test2"},
                new(){NameShortRu = "Test3"},
            }
        };
        gw.Children.Add(new GroupOfWells() { NameShortRu = "t1", ParentGroupOfWellsId = gw.id.ToString() });
        gw.Children.Add(new GroupOfWells() { NameShortRu = "t2", ParentGroupOfWellsId = gw.id.ToString() });
        gw.Children.Add(new GroupOfWells() { NameShortRu = "t3", ParentGroupOfWellsId = gw.id.ToString() });
        var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());
        var metadata = loader.GetMetadata().Result;

        var convertor = new GeneratedResourcesConvertor(new MemoryCache(new MemoryCacheOptions()), new MemoryCache(new MemoryCacheOptions()));
        var dict = convertor.Convert(gw).ConvertTest();

        var validator = new OpmValidator(new NullLogger<IOpmValidator>());

        var errors = validator.Validate(metadata, dict).ToList();
    }

    [Test]
    public void TestConvert()
    {
        var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());
        var metadata = loader.GetMetadata().Result;

        var well = new Well()
        {
            NameShortRu = "Well1",
            Bores = new List<Bore>()
            {
                new Bore() { NameShortRu = "B1" },
                new Bore() { NameShortRu = "B2" },
                new Bore() { NameShortRu = "B3" },
            }
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var convertor = new GeneratedResourcesConvertor(new MemoryCache(new MemoryCacheOptions()), new MemoryCache(new MemoryCacheOptions()));
        var dict = convertor.Convert(well).ConvertTest();

    }

    [Test]
    public void TestConvertNew()
    {
        var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());

        var metadata = loader.GetMetadata().Result;

        var well = new WellWithNew()
        {
            NameShortRu = "Well1",
            Bores = new List<Bore>()
            {
                new Bore() { NameShortRu = "B1" },
                new Bore() { NameShortRu = "B2" },
                new Bore() { NameShortRu = "B3" },
            }
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var convertor = new GeneratedResourcesConvertor(new MemoryCache(new MemoryCacheOptions()), new MemoryCache(new MemoryCacheOptions()));
        var dict = convertor.Convert(well).ConvertTest();

    }
}

class GroupOfWells
{
    [JsonPropertyName("ParentGroupOfWellsId")]
    public string? ParentGroupOfWellsId { get; set; }
    public Guid id { get; set; } = Guid.NewGuid();
    public List<GroupOfWells>? Children { get; set; }
    public string type { get; set; } = "GroupOfWells";
    public string NameShortRu { get; set; }
}

class Well
{
    public Guid id { get; set; } = Guid.NewGuid();
    public string type { get; set; } = "Well";
    public string NameShortRu { get; set; }

    public List<Bore> Bores { get; set; }
    public bool? needsMatch { get; set; }
}

class Bore
{
    public string NameShortRu { get; set; }
    public string type { get; set; } = "Bore";
    public Guid id { get; set; } = Guid.NewGuid();
}

abstract class WellBase
{
    public bool? needsMatch { get; set; }
}

class WellWithNew : WellBase
{
    public Guid id { get; set; } = Guid.NewGuid();
    public string type { get; set; } = "Well";
    public string NameShortRu { get; set; }

    public List<Bore> Bores { get; set; }
    public bool? needsMatch { get; set; }
}