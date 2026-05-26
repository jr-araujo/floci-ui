using Amazon.S3;
using Amazon.S3.Model;
using FlociDashboard.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace FlociDashboard.Tests.Services;

public class FlociServiceS3Tests
{
    private readonly FlociServiceBuilder _builder = new();

    [Fact]
    public async Task ListBucketsAsync_ReturnsMappedBuckets()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new() { BucketName = "my-bucket", CreationDate = DateTime.UtcNow }]
            });

        _builder.S3
            .Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                KeyCount = 3,
                S3Objects = [new() { Size = 512 }, new() { Size = 256 }, new() { Size = 256 }]
            });

        var service = _builder.Build();
        var result = await service.ListBucketsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("my-bucket");
        result[0].ObjectCount.Should().Be(3);
        result[0].TotalSizeBytes.Should().Be(1024);
    }

    [Fact]
    public async Task ListBucketsAsync_WhenObjectListingFails_StillReturnsBucketWithMinusOne()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new() { BucketName = "my-bucket", CreationDate = DateTime.UtcNow }]
            });

        _builder.S3
            .Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access Denied"));

        var service = _builder.Build();
        var result = await service.ListBucketsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("my-bucket");
        result[0].ObjectCount.Should().Be(-1);
    }

    [Fact]
    public async Task ListBucketsAsync_WhenNoBuckets_ReturnsEmptyList()
    {
        _builder.S3
            .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse { Buckets = [] });

        var service = _builder.Build();
        var result = await service.ListBucketsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBucketAsync_CallsPutBucketWithCorrectName()
    {
        _builder.S3
            .Setup(s => s.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketResponse());

        var service = _builder.Build();
        await service.CreateBucketAsync("new-bucket");

        _builder.S3.Verify(s => s.PutBucketAsync(
            It.Is<PutBucketRequest>(r => r.BucketName == "new-bucket"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBucketAsync_CallsDeleteBucketWithCorrectName()
    {
        _builder.S3
            .Setup(s => s.DeleteBucketAsync(It.IsAny<DeleteBucketRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteBucketResponse());

        var service = _builder.Build();
        await service.DeleteBucketAsync("old-bucket");

        _builder.S3.Verify(s => s.DeleteBucketAsync(
            It.Is<DeleteBucketRequest>(r => r.BucketName == "old-bucket"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListObjectsAsync_ReturnsMappedObjects()
    {
        var lastModified = DateTime.UtcNow;

        _builder.S3
            .Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [new() { Key = "file.txt", Size = 1024, LastModified = lastModified, ETag = "abc123" }]
            });

        var service = _builder.Build();
        var result = await service.ListObjectsAsync("my-bucket");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("file.txt");
        result[0].Size.Should().Be(1024);
        result[0].ETag.Should().Be("abc123");
    }

    [Fact]
    public async Task ListObjectsAsync_PassesPrefixToRequest()
    {
        _builder.S3
            .Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { S3Objects = [] });

        var service = _builder.Build();
        await service.ListObjectsAsync("my-bucket", "logs/");

        _builder.S3.Verify(s => s.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.BucketName == "my-bucket" && r.Prefix == "logs/"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteObjectAsync_CallsDeleteWithCorrectBucketAndKey()
    {
        _builder.S3
            .Setup(s => s.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        var service = _builder.Build();
        await service.DeleteObjectAsync("my-bucket", "file.txt");

        _builder.S3.Verify(s => s.DeleteObjectAsync("my-bucket", "file.txt", It.IsAny<CancellationToken>()), Times.Once);
    }
}
