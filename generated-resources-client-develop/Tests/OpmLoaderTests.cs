using GeneratedResourceClient.Opm;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class OpmLoaderTests
{
    [Test]
    public void TestLoadOpmTypesFromValidUrl()
    {
        var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());
        var metadata = loader.GetMetadata().Result;
        Assert.IsTrue(metadata != null);
    }

    [Test]
    public void TestLoadOpmTypesFromInValidUrl()
    {
        try
        {
            var loader = new OpmTypesDownloader(new Uri("http://100.111.0.49:1151/export"), new NullLogger<OpmTypesDownloader>());
            var metadata = loader.GetMetadata().Result;
            Assert.Fail();
        }
        catch (Exception e)
        {
            Assert.Pass();
        }
    }
}