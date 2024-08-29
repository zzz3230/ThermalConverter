using GeneratedResourceClient.GraphMaster.Preprocessor;
using GeneratedResourceClient.GraphMaster.Tools;
using GeneratedResourceClient.Opm;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nntc.ObjectModel;
using GeneratedResourceClient.Graph;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

internal static class ConvertEx
{
    public static IGeneratedResourcesCollection ConvertTest(this ObjectGraph graph) =>
        graph.ToTypedCollection((s, s1, _, _, _) => new TypeInfo((Multiplicity.One, Multiplicity.One), RelationshipDirection.Forward, (s, s1))).GeneratedResourcesCollection;

}

public class UploadPreprocessor
{
    public UploadPreprocessor()
    {
        var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());
        Metadata = loader.GetMetadata().Result;
        Preprocessor = new GeneratedResourceUploadPreprocessor(Metadata.Types.ToDictionary(x => x.Name));

    }

    public Metadata Metadata { get; set; }
    public GeneratedResourceUploadPreprocessor Preprocessor { get; set; }

    [Test]
    public void TestAddFields()
    {

        var items = new Well()
        {
            NameShortRu = "TestWell",
            needsMatch = true,
        };
        var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
        var converter = new GeneratedResourcesConvertor(cache, cache);
        var converted = converter.Convert(items);

        var preprocessed = Preprocessor.CreateReceiveModels(converted.TypedGroups);
        //Assert.IsTrue(preprocessed.First().needsMatch is true);
    }
    [Test]
    public void TestWithoutNeedsMatch()
    {
        var items = new Bore()
        {
            NameShortRu = "TestWell",
        };
        var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
        var converter = new GeneratedResourcesConvertor(cache, cache);
        var converted = converter.Convert(items).ConvertTest();

        var preprocessed = Preprocessor.CreateReceiveModels(converted);
        Assert.IsTrue(preprocessed.First().needsMatch is null);
    }
    [Test]
    public void TestSubTypes()
    {
        var laId = Guid.NewGuid();
        var items = new
        {
            BlockValveStationType = "Test",
            Type = "BlockValveStation",
            NameShortRu = "TestWell",
            LicenseAreaId = laId
        };

        var cache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
        var converter = new GeneratedResourcesConvertor(cache, cache);
        var test = converter.Convert(items);
        var tg = test.ConvertTest();

        var preprocessed = Preprocessor.CreateReceiveModels(tg);
        var bws = preprocessed.First().data["BlockValveStation"] as IDictionary<string, object>;
        var infr = preprocessed.First().data["InfrastructureFixedAssetSet"] as IDictionary<string, object>;
        Assert.IsTrue(bws["BlockValveStationType"] == "Test");
        Assert.IsTrue(infr["LicenseAreaId"].Equals(laId));
    }
}