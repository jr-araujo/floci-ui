using FlociDashboard.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FlociDashboard.Tests.Services;

public class RegionServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ISession> _session = new();

    private RegionService CreateService(Dictionary<string, string?>? values = null)
    {
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(_session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

        return new RegionService(_httpContextAccessor.Object, config);
    }

    [Fact]
    public void CurrentRegion_WhenSessionHasValue_ReturnsSessionValue()
    {
        var regionBytes = "eu-west-1"u8.ToArray();
        _session.Setup(s => s.TryGetValue("SelectedRegion", out regionBytes)).Returns(true);

        var service = CreateService();

        service.CurrentRegion.Should().Be("eu-west-1");
    }

    [Fact]
    public void CurrentRegion_WhenSessionIsEmpty_ReturnsConfigValue()
    {
        byte[]? noValue = null;
        _session.Setup(s => s.TryGetValue("SelectedRegion", out noValue)).Returns(false);

        var service = CreateService(new Dictionary<string, string?>
        {
            ["Floci:Region"] = "ap-southeast-1"
        });

        service.CurrentRegion.Should().Be("ap-southeast-1");
    }

    [Fact]
    public void CurrentRegion_WhenSessionAndConfigAreMissing_ReturnsFallbackRegion()
    {
        byte[]? noValue = null;
        _session.Setup(s => s.TryGetValue("SelectedRegion", out noValue)).Returns(false);

        var service = CreateService();

        service.CurrentRegion.Should().Be("us-east-1");
    }

    [Fact]
    public void CurrentRegion_Set_WritesToSession()
    {
        var service = CreateService();
        service.CurrentRegion = "sa-east-1";

        _session.Verify(s => s.Set(
            "SelectedRegion",
            It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == "sa-east-1")),
            Times.Once);
    }

    [Fact]
    public void BuildArn_ReturnsCorrectlyFormattedArn()
    {
        byte[]? noValue = null;
        _session.Setup(s => s.TryGetValue("SelectedRegion", out noValue)).Returns(false);

        var service = CreateService(new Dictionary<string, string?>
        {
            ["Floci:Region"] = "us-east-1"
        });
        var arn = service.BuildArn("s3", "bucket", "my-bucket");

        arn.Should().Be("arn:aws:s3:us-east-1:000000000000:bucket/my-bucket");
    }

    [Fact]
    public void AvailableRegions_WhenConfigured_ReturnsConfiguredList()
    {
        var regions = new List<string> { "us-east-1", "eu-west-1" };
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Floci:AvailableRegions:0"] = "us-east-1",
            ["Floci:AvailableRegions:1"] = "eu-west-1"
        });

        service.AvailableRegions.Should().BeEquivalentTo(regions);
    }

    [Fact]
    public void AvailableRegions_WhenConfigMissing_ReturnsFallbackList()
    {
        var service = CreateService();

        service.AvailableRegions.Should().ContainSingle().Which.Should().Be("us-east-1");
    }

    [Fact]
    public void AccountId_ReturnsExpectedValue()
    {
        var service = CreateService();

        service.AccountId.Should().Be("000000000000");
    }
}
