using FlociDashboard.Controllers;
using FlociDashboard.Models;
using FlociDashboard.Services;
using FlociDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlociDashboard.Tests.Controllers;

public class HomeControllerTests
{
    private readonly FlociServiceBuilder _builder = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<HomeController>> _logger = new();

    private static IConfiguration CreateRegionConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Floci:Region"] = "us-east-1",
                ["Floci:AvailableRegions:0"] = "us-east-1",
                ["Floci:AvailableRegions:1"] = "eu-west-1"
            })
            .Build();

    private HomeController CreateController()
    {
        var session = new Mock<ISession>();
        byte[]? noValue = null;
        session.Setup(s => s.TryGetValue(It.IsAny<string>(), out noValue)).Returns(false);
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

        var flociService = _builder.Build();
        var regionService = new RegionService(_httpContextAccessor.Object, CreateRegionConfiguration());

        var controller = new HomeController(flociService, regionService, _logger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithDashboardSummary()
    {
        SetupAllCountMocks();
        var controller = CreateController();

        var result = await controller.Index();

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<DashboardSummary>();
    }

    [Fact]
    public async Task Index_WhenServiceThrows_ReturnsViewWithEmptySummary()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("AWS unavailable"));

        var controller = CreateController();
        var result = await controller.Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<DashboardSummary>();
    }

    [Fact]
    public async Task Index_SetsFlociEndpointInViewBag()
    {
        SetupAllCountMocks();
        _builder.WithConfigValue("Floci:ServiceUrl", "http://localhost:4566");
        var controller = CreateController();

        await controller.Index();

        ((string?)controller.ViewBag.FlociEndpoint).Should().Be("http://localhost:4566");
    }

    [Fact]
    public async Task Index_SetsCurrentRegionInViewBag()
    {
        SetupAllCountMocks();
        var controller = CreateController();

        await controller.Index();

        ((string?)controller.ViewBag.CurrentRegion).Should().Be("us-east-1");
    }

    [Fact]
    public void SetRegion_RedirectsToIndex()
    {
        var controller = CreateController();

        var result = controller.SetRegion("eu-west-1", null);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public void SetRegion_WithValidReturnUrl_RedirectsToReturnUrl()
    {
        var session = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

        var flociService = _builder.Build();
        var regionService = new RegionService(_httpContextAccessor.Object, CreateRegionConfiguration());

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.IsLocalUrl("/dashboard")).Returns(true);

        var controller = new HomeController(flociService, regionService, _logger.Object);
        controller.Url = urlHelper.Object;

        var result = controller.SetRegion("eu-west-1", "/dashboard");

        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Be("/dashboard");
    }

    private void SetupAllCountMocks()
    {
        _builder.S3.Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.S3.Model.ListBucketsResponse { Buckets = [] });
        _builder.DynamoDB.Setup(d => d.ListTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.DynamoDBv2.Model.ListTablesResponse { TableNames = [] });
        _builder.SQS.Setup(s => s.ListQueuesAsync(It.IsAny<Amazon.SQS.Model.ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.SQS.Model.ListQueuesResponse { QueueUrls = [] });
        _builder.SNS.Setup(s => s.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.SimpleNotificationService.Model.ListTopicsResponse { Topics = [] });
        _builder.Lambda.Setup(l => l.ListFunctionsAsync(It.IsAny<Amazon.Lambda.Model.ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Lambda.Model.ListFunctionsResponse { Functions = [] });
        _builder.KMS.Setup(k => k.ListKeysAsync(It.IsAny<Amazon.KeyManagementService.Model.ListKeysRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.KeyManagementService.Model.ListKeysResponse { Keys = [] });
        _builder.IAM.Setup(i => i.ListUsersAsync(It.IsAny<Amazon.IdentityManagement.Model.ListUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.IdentityManagement.Model.ListUsersResponse { Users = [] });
        _builder.Secrets.Setup(s => s.ListSecretsAsync(It.IsAny<Amazon.SecretsManager.Model.ListSecretsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.SecretsManager.Model.ListSecretsResponse { SecretList = [] });
        _builder.Cognito.Setup(c => c.ListUserPoolsAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.ListUserPoolsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CognitoIdentityProvider.Model.ListUserPoolsResponse { UserPools = [] });
        _builder.Kinesis.Setup(k => k.ListStreamsAsync(It.IsAny<Amazon.Kinesis.Model.ListStreamsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Kinesis.Model.ListStreamsResponse { StreamNames = [] });
        _builder.Firehose.Setup(f => f.ListDeliveryStreamsAsync(It.IsAny<Amazon.KinesisFirehose.Model.ListDeliveryStreamsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.KinesisFirehose.Model.ListDeliveryStreamsResponse { DeliveryStreamNames = [] });
        _builder.SFN.Setup(s => s.ListStateMachinesAsync(It.IsAny<Amazon.StepFunctions.Model.ListStateMachinesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.StepFunctions.Model.ListStateMachinesResponse { StateMachines = [] });
        _builder.CFN.Setup(c => c.ListStacksAsync(It.IsAny<Amazon.CloudFormation.Model.ListStacksRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CloudFormation.Model.ListStacksResponse { StackSummaries = [] });
        _builder.EventBridge.Setup(e => e.ListEventBusesAsync(It.IsAny<Amazon.EventBridge.Model.ListEventBusesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.EventBridge.Model.ListEventBusesResponse { EventBuses = [] });
        _builder.Scheduler.Setup(s => s.ListSchedulesAsync(It.IsAny<Amazon.Scheduler.Model.ListSchedulesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Scheduler.Model.ListSchedulesResponse { Schedules = [] });
        _builder.CloudWatch.Setup(c => c.DescribeAlarmsAsync(It.IsAny<Amazon.CloudWatch.Model.DescribeAlarmsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CloudWatch.Model.DescribeAlarmsResponse { MetricAlarms = [] });
        _builder.CloudWatchLogs.Setup(c => c.DescribeLogGroupsAsync(It.IsAny<Amazon.CloudWatchLogs.Model.DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CloudWatchLogs.Model.DescribeLogGroupsResponse { LogGroups = [] });
        _builder.ElastiCache.Setup(e => e.DescribeCacheClustersAsync(It.IsAny<Amazon.ElastiCache.Model.DescribeCacheClustersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.ElastiCache.Model.DescribeCacheClustersResponse { CacheClusters = [] });
        _builder.RDS.Setup(r => r.DescribeDBInstancesAsync(It.IsAny<Amazon.RDS.Model.DescribeDBInstancesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.RDS.Model.DescribeDBInstancesResponse { DBInstances = [] });
        _builder.Glue.Setup(g => g.GetDatabasesAsync(It.IsAny<Amazon.Glue.Model.GetDatabasesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Glue.Model.GetDatabasesResponse { DatabaseList = [] });
        _builder.Athena.Setup(a => a.ListWorkGroupsAsync(It.IsAny<Amazon.Athena.Model.ListWorkGroupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Athena.Model.ListWorkGroupsResponse { WorkGroups = [] });
        _builder.ECS.Setup(e => e.ListClustersAsync(It.IsAny<Amazon.ECS.Model.ListClustersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.ECS.Model.ListClustersResponse { ClusterArns = [] });
        _builder.ECR.Setup(e => e.DescribeRepositoriesAsync(It.IsAny<Amazon.ECR.Model.DescribeRepositoriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.ECR.Model.DescribeRepositoriesResponse { Repositories = [] });
        _builder.ELB.Setup(e => e.DescribeLoadBalancersAsync(It.IsAny<Amazon.ElasticLoadBalancingV2.Model.DescribeLoadBalancersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.ElasticLoadBalancingV2.Model.DescribeLoadBalancersResponse { LoadBalancers = [] });
        _builder.AutoScaling.Setup(a => a.DescribeAutoScalingGroupsAsync(It.IsAny<Amazon.AutoScaling.Model.DescribeAutoScalingGroupsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.AutoScaling.Model.DescribeAutoScalingGroupsResponse { AutoScalingGroups = [] });
        _builder.Backup.Setup(b => b.ListBackupVaultsAsync(It.IsAny<Amazon.Backup.Model.ListBackupVaultsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Backup.Model.ListBackupVaultsResponse { BackupVaultList = [] });
        _builder.OpenSearch.Setup(o => o.ListDomainNamesAsync(It.IsAny<Amazon.OpenSearchService.Model.ListDomainNamesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.OpenSearchService.Model.ListDomainNamesResponse { DomainNames = [] });
        _builder.Route53.Setup(r => r.ListHostedZonesAsync(It.IsAny<Amazon.Route53.Model.ListHostedZonesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Route53.Model.ListHostedZonesResponse { HostedZones = [] });
        _builder.AppConfig.Setup(a => a.ListApplicationsAsync(It.IsAny<Amazon.AppConfig.Model.ListApplicationsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.AppConfig.Model.ListApplicationsResponse { Items = [] });
        _builder.Transfer.Setup(t => t.ListServersAsync(It.IsAny<Amazon.Transfer.Model.ListServersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.Transfer.Model.ListServersResponse { Servers = [] });
        _builder.SES.Setup(s => s.ListIdentitiesAsync(It.IsAny<Amazon.SimpleEmail.Model.ListIdentitiesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.SimpleEmail.Model.ListIdentitiesResponse { Identities = [] });
        _builder.SSM.Setup(s => s.DescribeParametersAsync(It.IsAny<Amazon.SimpleSystemsManagement.Model.DescribeParametersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.SimpleSystemsManagement.Model.DescribeParametersResponse { Parameters = [] });
    }
}
