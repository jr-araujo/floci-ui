using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using FlociDashboard.Controllers;
using FlociDashboard.Models;
using FlociDashboard.Services;
using FlociDashboard.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlociDashboard.Tests.Controllers;

public class ServicesControllerTests
{
    private readonly FlociServiceBuilder _builder = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<ServicesController>> _logger = new();

    private static IConfiguration CreateRegionConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Floci:Region"] = "us-east-1",
                ["Floci:AvailableRegions:0"] = "us-east-1",
                ["Floci:AvailableRegions:1"] = "eu-west-1"
            })
            .Build();

    private ServicesController CreateController()
    {
        var session = new Mock<ISession>();
        byte[]? noValue = null;
        session.Setup(s => s.TryGetValue(It.IsAny<string>(), out noValue)).Returns(false);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

        var flociService = _builder.Build();
        var regionService = new RegionService(_httpContextAccessor.Object, CreateRegionConfiguration());

        var controller = new ServicesController(flociService, regionService, _logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
        controller.TempData = new TempDataDictionary(httpContext.Object, Mock.Of<ITempDataProvider>());
        return controller;
    }

    // ─── S3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GivenBuckets_WhenS3_ThenReturnsViewWithBuckets()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new() { BucketName = "bucket-1", CreationDate = DateTime.UtcNow }]
            });
        _builder.S3
            .Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { KeyCount = 0, S3Objects = [] });

        var controller = CreateController();
        var result = await controller.S3();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<List<S3BucketInfo>>()
            .Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenS3ServiceFailure_WhenS3_ThenReturnsViewWithEmptyList()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Error"));

        var controller = CreateController();
        var result = await controller.S3();

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<List<S3BucketInfo>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenCreateBucketSucceeds_WhenCreateBucket_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.S3
            .Setup(s => s.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketResponse());

        var controller = CreateController();
        var result = await controller.CreateBucket(new CreateBucketViewModel { BucketName = "new-bucket" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("S3");
        controller.TempData["Success"].Should().Be("Bucket 'new-bucket' created.");
    }

    [Fact]
    public async Task GivenCreateBucketFails_WhenCreateBucket_ThenSetsErrorTempDataAndRedirects()
    {
        _builder.S3
            .Setup(s => s.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Already exists"));

        var controller = CreateController();
        var result = await controller.CreateBucket(new CreateBucketViewModel { BucketName = "new-bucket" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("S3");
        controller.TempData["Error"].Should().NotBeNull();
    }

    [Fact]
    public async Task GivenDeleteBucketSucceeds_WhenDeleteBucket_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.S3
            .Setup(s => s.DeleteBucketAsync(It.IsAny<DeleteBucketRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteBucketResponse());

        var controller = CreateController();
        var result = await controller.DeleteBucket("old-bucket");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("S3");
        controller.TempData["Success"].Should().Be("Bucket 'old-bucket' deleted.");
    }

    // ─── DynamoDB ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GivenTables_WhenDynamoDB_ThenReturnsViewWithTables()
    {
        _builder.DynamoDB
            .Setup(d => d.ListTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTablesResponse { TableNames = ["orders"] });
        _builder.DynamoDB
            .Setup(d => d.DescribeTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeTableResponse
            {
                Table = new TableDescription
                {
                    TableName = "orders",
                    TableStatus = TableStatus.ACTIVE,
                    KeySchema = [new() { AttributeName = "id", KeyType = Amazon.DynamoDBv2.KeyType.HASH }]
                }
            });

        var controller = CreateController();
        var result = await controller.DynamoDB();

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<List<DynamoTableInfo>>()
            .Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenCreateTableSucceeds_WhenCreateTable_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.DynamoDB
            .Setup(d => d.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTableResponse());

        var controller = CreateController();
        var result = await controller.CreateTable(new CreateTableViewModel { TableName = "users", PartitionKey = "id" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("DynamoDB");
        controller.TempData["Success"].Should().Be("Table 'users' created.");
    }

    [Fact]
    public async Task GivenDeleteTableSucceeds_WhenDeleteTable_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.DynamoDB
            .Setup(d => d.DeleteTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteTableResponse());

        var controller = CreateController();
        var result = await controller.DeleteTable("old-table");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("DynamoDB");
        controller.TempData["Success"].Should().Be("Table 'old-table' deleted.");
    }

    // ─── SQS ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GivenQueues_WhenSQS_ThenReturnsViewWithQueues()
    {
        _builder.SQS
            .Setup(s => s.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListQueuesResponse { QueueUrls = [] });

        var controller = CreateController();
        var result = await controller.SQS();

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<List<SqsQueueInfo>>();
    }

    [Fact]
    public async Task GivenCreateQueueSucceeds_WhenCreateQueue_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.SQS
            .Setup(s => s.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateQueueResponse());

        var controller = CreateController();
        var result = await controller.CreateQueue(new CreateQueueViewModel { QueueName = "my-queue" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("SQS");
        controller.TempData["Success"].Should().Be("Queue 'my-queue' created.");
    }

    [Fact]
    public async Task GivenDeleteQueueSucceeds_WhenDeleteQueue_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.SQS
            .Setup(s => s.DeleteQueueAsync(It.IsAny<DeleteQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteQueueResponse());

        var controller = CreateController();
        var result = await controller.DeleteQueue("https://sqs/000/my-queue");

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("SQS");
        controller.TempData["Success"].Should().Be("Queue 'my-queue' deleted.");
    }

    [Fact]
    public async Task GivenSendMessageSucceeds_WhenSendMessage_ThenSetsSuccessTempDataAndRedirects()
    {
        _builder.SQS
            .Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        var controller = CreateController();
        var result = await controller.SendMessage(new SendMessageViewModel { QueueUrl = "https://sqs/q", Body = "hello" });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("SQS");
        controller.TempData["Success"].Should().Be("Message sent successfully.");
    }
}
