using Amazon.SQS;
using Amazon.SQS.Model;
using FlociDashboard.Models;
using FlociDashboard.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace FlociDashboard.Tests.Services;

public class FlociServiceSqsTests
{
    private readonly FlociServiceBuilder _builder = new();

    [Fact]
    public async Task GivenQueueAttributes_WhenListQueuesAsync_ThenReturnsMappedQueues()
    {
        _builder.SQS
            .Setup(s => s.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListQueuesResponse
            {
                QueueUrls = ["https://sqs.us-east-1.amazonaws.com/000000000000/my-queue"]
            });

        _builder.SQS
            .Setup(s => s.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["QueueArn"] = "arn:aws:sqs:us-east-1:000000000000:my-queue",
                    ["ApproximateNumberOfMessages"] = "5",
                    ["ApproximateNumberOfMessagesNotVisible"] = "2",
                    ["VisibilityTimeout"] = "30",
                    ["MessageRetentionPeriod"] = "345600"
                }
            });

        var service = _builder.Build();
        var result = await service.ListQueuesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("my-queue");
        result[0].MessagesAvailable.Should().Be(5);
        result[0].MessagesInFlight.Should().Be(2);
        result[0].VisibilityTimeout.Should().Be(30);
        result[0].IsFifo.Should().BeFalse();
    }

    [Fact]
    public async Task GivenAttributeReadFails_WhenListQueuesAsync_ThenReturnsQueueWithName()
    {
        _builder.SQS
            .Setup(s => s.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListQueuesResponse
            {
                QueueUrls = ["https://sqs.us-east-1.amazonaws.com/000000000000/broken-queue"]
            });

        _builder.SQS
            .Setup(s => s.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Access Denied"));

        var service = _builder.Build();
        var result = await service.ListQueuesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("broken-queue");
    }

    [Fact]
    public async Task GivenFifoQueueUrl_WhenListQueuesAsync_ThenSetsIsFifoTrue()
    {
        _builder.SQS
            .Setup(s => s.ListQueuesAsync(It.IsAny<ListQueuesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListQueuesResponse
            {
                QueueUrls = ["https://sqs.us-east-1.amazonaws.com/000000000000/orders.fifo"]
            });

        _builder.SQS
            .Setup(s => s.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { Attributes = [] });

        var service = _builder.Build();
        var result = await service.ListQueuesAsync();

        result[0].IsFifo.Should().BeTrue();
    }

    [Fact]
    public async Task GivenStandardQueueModel_WhenCreateQueueAsync_ThenCallsCreateQueueWithCorrectName()
    {
        _builder.SQS
            .Setup(s => s.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateQueueResponse());

        var model = new CreateQueueViewModel { QueueName = "my-queue", VisibilityTimeout = 30, MessageRetentionPeriod = 345600 };

        var service = _builder.Build();
        await service.CreateQueueAsync(model);

        _builder.SQS.Verify(s => s.CreateQueueAsync(
            It.Is<CreateQueueRequest>(r => r.QueueName == "my-queue"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenFifoQueueModel_WhenCreateQueueAsync_ThenAppendsFifoSuffix()
    {
        _builder.SQS
            .Setup(s => s.CreateQueueAsync(It.IsAny<CreateQueueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateQueueResponse());

        var model = new CreateQueueViewModel { QueueName = "orders", Fifo = true, VisibilityTimeout = 30, MessageRetentionPeriod = 345600 };

        var service = _builder.Build();
        await service.CreateQueueAsync(model);

        _builder.SQS.Verify(s => s.CreateQueueAsync(
            It.Is<CreateQueueRequest>(r => r.QueueName == "orders.fifo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenQueueUrl_WhenDeleteQueueAsync_ThenCallsDeleteWithCorrectUrl()
    {
        _builder.SQS
            .Setup(s => s.DeleteQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteQueueResponse());

        var service = _builder.Build();
        await service.DeleteQueueAsync("https://sqs.us-east-1.amazonaws.com/000000000000/my-queue");

        _builder.SQS.Verify(s => s.DeleteQueueAsync(
            "https://sqs.us-east-1.amazonaws.com/000000000000/my-queue",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenReceivedMessages_WhenReceiveMessagesAsync_ThenReturnsMappedMessages()
    {
        var sentAt = DateTimeOffset.UtcNow;

        _builder.SQS
            .Setup(s => s.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse
            {
                Messages =
                [
                    new()
                    {
                        MessageId = "msg-1",
                        ReceiptHandle = "receipt-1",
                        Body = "hello",
                        Attributes = new Dictionary<string, string>
                        {
                            ["SentTimestamp"] = sentAt.ToUnixTimeMilliseconds().ToString(),
                            ["ApproximateReceiveCount"] = "1",
                            ["SenderId"] = "sender-123"
                        }
                    }
                ]
            });

        var model = new ReceiveMessagesViewModel { QueueUrl = "https://sqs/q", QueueName = "my-queue", MaxMessages = 5 };

        var service = _builder.Build();
        var result = await service.ReceiveMessagesAsync(model);

        result.Should().HaveCount(1);
        result[0].MessageId.Should().Be("msg-1");
        result[0].Body.Should().Be("hello");
        result[0].ApproximateReceiveCount.Should().Be(1);
        result[0].QueueName.Should().Be("my-queue");
    }

    [Fact]
    public async Task GivenSendMessageModel_WhenSendMessageAsync_ThenCallsSendWithCorrectBody()
    {
        _builder.SQS
            .Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        var model = new SendMessageViewModel { QueueUrl = "https://sqs/q", Body = "test message" };

        var service = _builder.Build();
        await service.SendMessageAsync(model);

        _builder.SQS.Verify(s => s.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.QueueUrl == "https://sqs/q" && r.MessageBody == "test message"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenQueueAndReceiptHandle_WhenDeleteMessageAsync_ThenCallsDeleteWithCorrectReceiptHandle()
    {
        _builder.SQS
            .Setup(s => s.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var service = _builder.Build();
        await service.DeleteMessageAsync("https://sqs/q", "receipt-abc");

        _builder.SQS.Verify(s => s.DeleteMessageAsync(
            It.Is<DeleteMessageRequest>(r => r.QueueUrl == "https://sqs/q" && r.ReceiptHandle == "receipt-abc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
