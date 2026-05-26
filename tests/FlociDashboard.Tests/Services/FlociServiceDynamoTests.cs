using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FlociDashboard.Models;
using FlociDashboard.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace FlociDashboard.Tests.Services;

public class FlociServiceDynamoTests
{
    private readonly FlociServiceBuilder _builder = new();

    [Fact]
    public async Task ListTablesAsync_ReturnsMappedTables()
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
                    ItemCount = 100,
                    TableSizeBytes = 2048,
                    BillingModeSummary = new BillingModeSummary { BillingMode = BillingMode.PAY_PER_REQUEST },
                    KeySchema = [new() { AttributeName = "id", KeyType = Amazon.DynamoDBv2.KeyType.HASH }]
                }
            });

        var service = _builder.Build();
        var result = await service.ListTablesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("orders");
        result[0].Status.Should().Be("ACTIVE");
        result[0].ItemCount.Should().Be(100);
        result[0].BillingMode.Should().Be("PAY_PER_REQUEST");
        result[0].KeySchema.Should().ContainSingle().Which.Should().Contain("id");
    }

    [Fact]
    public async Task ListTablesAsync_WhenDescribeFails_ReturnsUnknownStatus()
    {
        _builder.DynamoDB
            .Setup(d => d.ListTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTablesResponse { TableNames = ["broken-table"] });

        _builder.DynamoDB
            .Setup(d => d.DescribeTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceNotFoundException("Not found"));

        var service = _builder.Build();
        var result = await service.ListTablesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("broken-table");
        result[0].Status.Should().Be("UNKNOWN");
    }

    [Fact]
    public async Task CreateTableAsync_WithPartitionKeyOnly_CallsCreateTable()
    {
        _builder.DynamoDB
            .Setup(d => d.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTableResponse());

        var service = _builder.Build();
        await service.CreateTableAsync("users", "id");

        _builder.DynamoDB.Verify(d => d.CreateTableAsync(
            It.Is<CreateTableRequest>(r =>
                r.TableName == "users" &&
                r.KeySchema.Count == 1 &&
                r.KeySchema[0].AttributeName == "id"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTableAsync_WithSortKey_AddsRangeKeyToSchema()
    {
        _builder.DynamoDB
            .Setup(d => d.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTableResponse());

        var service = _builder.Build();
        await service.CreateTableAsync("orders", "pk", "sk");

        _builder.DynamoDB.Verify(d => d.CreateTableAsync(
            It.Is<CreateTableRequest>(r =>
                r.KeySchema.Count == 2 &&
                r.KeySchema.Any(k => k.AttributeName == "sk" && k.KeyType == Amazon.DynamoDBv2.KeyType.RANGE)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTableAsync_WithStreamsEnabled_SetsStreamSpecification()
    {
        _builder.DynamoDB
            .Setup(d => d.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTableResponse());

        var service = _builder.Build();
        await service.CreateTableAsync("events", "id", enableStreams: true);

        _builder.DynamoDB.Verify(d => d.CreateTableAsync(
            It.Is<CreateTableRequest>(r =>
                r.StreamSpecification != null &&
                r.StreamSpecification.StreamEnabled == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTableAsync_CallsDeleteWithCorrectName()
    {
        _builder.DynamoDB
            .Setup(d => d.DeleteTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteTableResponse());

        var service = _builder.Build();
        await service.DeleteTableAsync("old-table");

        _builder.DynamoDB.Verify(d => d.DeleteTableAsync("old-table", It.IsAny<CancellationToken>()), Times.Once);
    }
}
