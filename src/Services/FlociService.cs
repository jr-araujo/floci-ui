using Amazon.AppConfig;
using Amazon.AppConfig.Model;
using Amazon.Athena;
using Amazon.Athena.Model;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.Backup;
using Amazon.Backup.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.ElastiCache;
using Amazon.ElastiCache.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Glue;
using Amazon.Glue.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.OpenSearchService;
using Amazon.OpenSearchService.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Scheduler;
using Amazon.Scheduler.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Amazon.Transfer;
using Amazon.Transfer.Model;
using AppConfigCreateApplicationRequest = Amazon.AppConfig.Model.CreateApplicationRequest;
using AppConfigDeleteApplicationRequest = Amazon.AppConfig.Model.DeleteApplicationRequest;
using AppConfigListApplicationsRequest = Amazon.AppConfig.Model.ListApplicationsRequest;
using DynamoCreateTableRequest = Amazon.DynamoDBv2.Model.CreateTableRequest;
using DynamoKeySchemaElement = Amazon.DynamoDBv2.Model.KeySchemaElement;
using DynamoKeyType = Amazon.DynamoDBv2.KeyType;
using EcsLaunchType = Amazon.ECS.LaunchType;
using ElbDescribeLoadBalancersRequest = Amazon.ElasticLoadBalancingV2.Model.DescribeLoadBalancersRequest;
using ElbDescribeTargetGroupsRequest = Amazon.ElasticLoadBalancingV2.Model.DescribeTargetGroupsRequest;
using EventBridgeDeleteRuleRequest = Amazon.EventBridge.Model.DeleteRuleRequest;
using IamCreateUserRequest = Amazon.IdentityManagement.Model.CreateUserRequest;
using IamDeleteUserRequest = Amazon.IdentityManagement.Model.DeleteUserRequest;
using IamListUsersRequest = Amazon.IdentityManagement.Model.ListUsersRequest;
using KinesisListStreamsRequest = Amazon.Kinesis.Model.ListStreamsRequest;
using KinesisPutRecordRequest = Amazon.Kinesis.Model.PutRecordRequest;
using LambdaInvocationType = Amazon.Lambda.InvocationType;
using SchedulerScheduleState = Amazon.Scheduler.ScheduleState;
using SesDestination = Amazon.SimpleEmail.Model.Destination;
using SfnListExecutionsRequest = Amazon.StepFunctions.Model.ListExecutionsRequest;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using TransferIdentityProviderType = Amazon.Transfer.IdentityProviderType;
using TransferProtocol = Amazon.Transfer.Protocol;
using FlociDashboard.Models;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace FlociDashboard.Services;

public class FlociService(
    IAmazonS3 s3,
    IAmazonDynamoDB dynamo,
    IAmazonSQS sqs,
    IAmazonSimpleNotificationService sns,
    IAmazonLambda lambda,
    IAmazonKeyManagementService kms,
    IAmazonIdentityManagementService iam,
    IAmazonSimpleSystemsManagement ssm,
    IAmazonSecretsManager secrets,
    IAmazonSimpleEmailService ses,
    IAmazonCognitoIdentityProvider cognito,
    IAmazonKinesis kinesis,
    IAmazonKinesisFirehose firehose,
    IAmazonStepFunctions sfn,
    IAmazonCloudFormation cfn,
    IAmazonEventBridge eventBridge,
    IAmazonScheduler scheduler,
    IAmazonCloudWatch cloudWatch,
    IAmazonCloudWatchLogs cloudWatchLogs,
    IAmazonElastiCache elastiCache,
    IAmazonRDS rds,
    IAmazonGlue glue,
    IAmazonAthena athena,
    IAmazonECS ecs,
    IAmazonECR ecr,
    IAmazonElasticLoadBalancingV2 elb,
    IAmazonAutoScaling autoScaling,
    IAmazonBackup backup,
    IAmazonOpenSearchService openSearch,
    IAmazonRoute53 route53,
    IAmazonAppConfig appConfig,
    IAmazonTransfer transfer,
    IConfiguration config,
    ILogger<FlociService> logger)
{
    public string FlociEndpoint => config["Floci:ServiceUrl"] ?? "http://localhost:4566";

    private async Task<int> SafeCountAsync(Func<Task<int>> fn)
    {
        try { return await fn(); }
        catch { return -1; }
    }

    // ─── Dashboard Summary ──────────────────────────────────────────────────────

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var tasks = await Task.WhenAll(
            // Storage
            SafeCountAsync(() => GetS3BucketCountAsync()),
            SafeCountAsync(() => GetDynamoTableCountAsync()),
            SafeCountAsync(() => GetElastiCacheClusterCountAsync()),
            SafeCountAsync(() => GetRdsInstanceCountAsync()),
            SafeCountAsync(() => GetOpenSearchDomainCountAsync()),
            SafeCountAsync(() => GetBackupVaultCountAsync()),
            // Messaging
            SafeCountAsync(() => GetSqsQueueCountAsync()),
            SafeCountAsync(() => GetSnsTopicCountAsync()),
            SafeCountAsync(() => GetKinesisStreamCountAsync()),
            SafeCountAsync(() => GetFirehoseStreamCountAsync()),
            SafeCountAsync(() => GetSesIdentityCountAsync()),
            SafeCountAsync(() => GetEventBridgeBusCountAsync()),
            SafeCountAsync(() => GetSchedulerScheduleCountAsync()),
            // Compute
            SafeCountAsync(() => GetLambdaFunctionCountAsync()),
            SafeCountAsync(() => GetEcsClusterCountAsync()),
            SafeCountAsync(() => GetEcrRepositoryCountAsync()),
            SafeCountAsync(() => GetStateMachineCountAsync()),
            SafeCountAsync(() => GetElbCountAsync()),
            SafeCountAsync(() => GetAutoScalingGroupCountAsync()),
            // Security
            SafeCountAsync(() => GetKmsKeyCountAsync()),
            SafeCountAsync(() => GetIamUserCountAsync()),
            SafeCountAsync(() => GetCognitoUserPoolCountAsync()),
            SafeCountAsync(() => GetSecretsCountAsync()),
            // Management
            SafeCountAsync(() => GetSsmParameterCountAsync()),
            SafeCountAsync(() => GetCloudFormationStackCountAsync()),
            SafeCountAsync(() => GetCloudWatchAlarmCountAsync()),
            SafeCountAsync(() => GetCloudWatchLogGroupCountAsync()),
            SafeCountAsync(() => GetAppConfigApplicationCountAsync()),
            // Analytics
            SafeCountAsync(() => GetGlueDatabaseCountAsync()),
            SafeCountAsync(() => GetAthenaWorkGroupCountAsync()),
            // Network
            SafeCountAsync(() => GetRoute53ZoneCountAsync()),
            SafeCountAsync(() => GetTransferServerCountAsync())
        );
        return new DashboardSummary
        {
            // Storage
            S3Buckets = tasks[0], DynamoTables = tasks[1], ElastiCacheClusters = tasks[2],
            RdsInstances = tasks[3], OpenSearchDomains = tasks[4], BackupVaults = tasks[5],
            // Messaging
            SqsQueues = tasks[6], SnsTopics = tasks[7], KinesisStreams = tasks[8],
            FirehoseStreams = tasks[9], SesIdentities = tasks[10], EventBridgeBuses = tasks[11],
            SchedulerSchedules = tasks[12],
            // Compute
            LambdaFunctions = tasks[13], EcsClusters = tasks[14], EcrRepositories = tasks[15],
            StepFunctionStateMachines = tasks[16], ElbLoadBalancers = tasks[17], AutoScalingGroups = tasks[18],
            // Security
            KmsKeys = tasks[19], IamUsers = tasks[20], CognitoUserPools = tasks[21], SecretsCount = tasks[22],
            // Management
            SsmParameters = tasks[23], CloudFormationStacks = tasks[24], CloudWatchAlarms = tasks[25],
            CloudWatchLogGroups = tasks[26], AppConfigApplications = tasks[27],
            // Analytics
            GlueDatabases = tasks[28], AthenaWorkGroups = tasks[29],
            // Network
            Route53Zones = tasks[30], TransferServers = tasks[31],
            LastRefreshed = DateTime.UtcNow
        };
    }

    // ─── S3 ────────────────────────────────────────────────────────────────────

    private async Task<int> GetS3BucketCountAsync() => (await s3.ListBucketsAsync()).Buckets.Count;

    public async Task<List<S3BucketInfo>> ListBucketsAsync()
    {
        var resp = await s3.ListBucketsAsync();
        var result = new List<S3BucketInfo>();
        foreach (var b in resp.Buckets)
        {
            int objectCount = -1; long totalSize = 0;
            try
            {
                var objects = await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = b.BucketName });
                objectCount = objects.KeyCount;
                totalSize = objects.S3Objects.Sum(o => o.Size);
            }
            catch { }
            result.Add(new S3BucketInfo { Name = b.BucketName, CreationDate = b.CreationDate, ObjectCount = objectCount, TotalSizeBytes = totalSize });
        }
        return result;
    }

    public async Task CreateBucketAsync(string name) => await s3.PutBucketAsync(new PutBucketRequest { BucketName = name });
    public async Task DeleteBucketAsync(string name) => await s3.DeleteBucketAsync(new DeleteBucketRequest { BucketName = name });

    public async Task<List<S3ObjectInfo>> ListObjectsAsync(string bucket, string? prefix = null)
    {
        var resp = await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = prefix });
        return resp.S3Objects.Select(o => new S3ObjectInfo { Key = o.Key, Size = o.Size, LastModified = o.LastModified, ETag = o.ETag, StorageClass = o.StorageClass }).ToList();
    }

    public async Task DeleteObjectAsync(string bucket, string key) => await s3.DeleteObjectAsync(bucket, key);

    // ─── DynamoDB ──────────────────────────────────────────────────────────────

    private async Task<int> GetDynamoTableCountAsync() => (await dynamo.ListTablesAsync()).TableNames.Count;

    public async Task<List<DynamoTableInfo>> ListTablesAsync()
    {
        var resp = await dynamo.ListTablesAsync();
        var result = new List<DynamoTableInfo>();
        foreach (var name in resp.TableNames)
        {
            try
            {
                var desc = await dynamo.DescribeTableAsync(name);
                var t = desc.Table;
                result.Add(new DynamoTableInfo
                {
                    Name = t.TableName, Status = t.TableStatus.Value, ItemCount = t.ItemCount,
                    SizeBytes = t.TableSizeBytes, BillingMode = t.BillingModeSummary?.BillingMode?.Value ?? "PROVISIONED",
                    KeySchema = t.KeySchema.Select(k => $"{k.AttributeName} ({k.KeyType.Value})").ToList(),
                    StreamEnabled = t.StreamSpecification?.StreamEnabled ?? false,
                    StreamViewType = t.StreamSpecification?.StreamViewType?.Value ?? ""
                });
            }
            catch { result.Add(new DynamoTableInfo { Name = name, Status = "UNKNOWN" }); }
        }
        return result;
    }

    public async Task CreateTableAsync(string name, string partitionKey, string sortKey = "", bool enableStreams = false, string streamViewType = "NEW_AND_OLD_IMAGES")
    {
        var attrs = new List<AttributeDefinition> { new() { AttributeName = partitionKey, AttributeType = ScalarAttributeType.S } };
        var schema = new List<DynamoKeySchemaElement> { new() { AttributeName = partitionKey, KeyType = DynamoKeyType.HASH } };
        if (!string.IsNullOrWhiteSpace(sortKey))
        {
            attrs.Add(new() { AttributeName = sortKey, AttributeType = ScalarAttributeType.S });
            schema.Add(new() { AttributeName = sortKey, KeyType = DynamoKeyType.RANGE });
        }
        var req = new DynamoCreateTableRequest
        {
            TableName = name, AttributeDefinitions = attrs, KeySchema = schema,
            BillingMode = BillingMode.PAY_PER_REQUEST
        };
        if (enableStreams)
            req.StreamSpecification = new StreamSpecification { StreamEnabled = true, StreamViewType = new StreamViewType(streamViewType) };
        await dynamo.CreateTableAsync(req);
    }

    public async Task DeleteTableAsync(string name) => await dynamo.DeleteTableAsync(name);

    // ─── SQS ───────────────────────────────────────────────────────────────────

    private async Task<int> GetSqsQueueCountAsync() => (await sqs.ListQueuesAsync(new ListQueuesRequest())).QueueUrls.Count;

    // ─── Additional count helpers ──────────────────────────────────────────────
    private async Task<int> GetFirehoseStreamCountAsync() => (await firehose.ListDeliveryStreamsAsync(new ListDeliveryStreamsRequest())).DeliveryStreamNames.Count;
    private async Task<int> GetSesIdentityCountAsync() => (await ses.ListIdentitiesAsync(new ListIdentitiesRequest())).Identities.Count;
    private async Task<int> GetSchedulerScheduleCountAsync() => (await scheduler.ListSchedulesAsync(new ListSchedulesRequest())).Schedules.Count;
    private async Task<int> GetElbCountAsync() => (await elb.DescribeLoadBalancersAsync(new ElbDescribeLoadBalancersRequest())).LoadBalancers.Count;
    private async Task<int> GetAutoScalingGroupCountAsync() => (await autoScaling.DescribeAutoScalingGroupsAsync(new DescribeAutoScalingGroupsRequest())).AutoScalingGroups.Count;
    private async Task<int> GetCloudFormationStackCountAsync() => (await cfn.ListStacksAsync(new ListStacksRequest())).StackSummaries.Count(s => s.StackStatus != Amazon.CloudFormation.StackStatus.DELETE_COMPLETE);
    private async Task<int> GetCloudWatchAlarmCountAsync() => (await cloudWatch.DescribeAlarmsAsync(new DescribeAlarmsRequest())).MetricAlarms.Count;
    private async Task<int> GetCloudWatchLogGroupCountAsync() => (await cloudWatchLogs.DescribeLogGroupsAsync(new DescribeLogGroupsRequest())).LogGroups.Count;
    private async Task<int> GetAppConfigApplicationCountAsync() => (await appConfig.ListApplicationsAsync(new AppConfigListApplicationsRequest())).Items.Count;
    private async Task<int> GetGlueDatabaseCountAsync() => (await glue.GetDatabasesAsync(new Amazon.Glue.Model.GetDatabasesRequest())).DatabaseList.Count;
    private async Task<int> GetAthenaWorkGroupCountAsync() => (await athena.ListWorkGroupsAsync(new Amazon.Athena.Model.ListWorkGroupsRequest())).WorkGroups.Count;
    private async Task<int> GetRoute53ZoneCountAsync() => (await route53.ListHostedZonesAsync(new ListHostedZonesRequest())).HostedZones.Count;
    private async Task<int> GetTransferServerCountAsync() => (await transfer.ListServersAsync(new ListServersRequest())).Servers.Count;
    private async Task<int> GetOpenSearchDomainCountAsync() => (await openSearch.ListDomainNamesAsync(new ListDomainNamesRequest())).DomainNames.Count;
    private async Task<int> GetBackupVaultCountAsync() => (await backup.ListBackupVaultsAsync(new ListBackupVaultsRequest())).BackupVaultList.Count;

    public async Task<List<SqsQueueInfo>> ListQueuesAsync()
    {
        var resp = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var result = new List<SqsQueueInfo>();
        foreach (var url in resp.QueueUrls)
        {
            try
            {
                var attrs = await sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = url,
                    AttributeNames = ["All"]
                });
                var a = attrs.Attributes;
                string? dlqArn = null; int maxReceive = 0;
                if (a.TryGetValue("RedrivePolicy", out var rdp) && !string.IsNullOrEmpty(rdp))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(rdp);
                        dlqArn = doc.RootElement.GetProperty("deadLetterTargetArn").GetString();
                        maxReceive = doc.RootElement.GetProperty("maxReceiveCount").GetInt32();
                    }
                    catch { }
                }
                result.Add(new SqsQueueInfo
                {
                    Name = url.Split('/').Last(), Url = url,
                    Arn = a.GetValueOrDefault("QueueArn", ""),
                    MessagesAvailable = int.TryParse(a.GetValueOrDefault("ApproximateNumberOfMessages"), out var m) ? m : 0,
                    MessagesInFlight = int.TryParse(a.GetValueOrDefault("ApproximateNumberOfMessagesNotVisible"), out var mf) ? mf : 0,
                    VisibilityTimeout = int.TryParse(a.GetValueOrDefault("VisibilityTimeout"), out var vt) ? vt : 0,
                    RetentionPeriod = int.TryParse(a.GetValueOrDefault("MessageRetentionPeriod"), out var rp) ? rp : 0,
                    IsFifo = url.EndsWith(".fifo"),
                    DeadLetterTargetArn = dlqArn,
                    MaxReceiveCount = maxReceive
                });
            }
            catch { result.Add(new SqsQueueInfo { Name = url.Split('/').Last(), Url = url }); }
        }
        return result;
    }

    public async Task CreateQueueAsync(CreateQueueViewModel model)
    {
        var qName = model.Fifo && !model.QueueName.EndsWith(".fifo") ? model.QueueName + ".fifo" : model.QueueName;
        var req = new CreateQueueRequest { QueueName = qName };
        req.Attributes["VisibilityTimeout"] = model.VisibilityTimeout.ToString();
        req.Attributes["MessageRetentionPeriod"] = model.MessageRetentionPeriod.ToString();
        if (model.Fifo)
        {
            req.Attributes["FifoQueue"] = "true";
            req.Attributes["ContentBasedDeduplication"] = "true";
        }
        if (!string.IsNullOrWhiteSpace(model.DeadLetterQueueArn))
        {
            req.Attributes["RedrivePolicy"] = JsonSerializer.Serialize(new { deadLetterTargetArn = model.DeadLetterQueueArn, maxReceiveCount = model.MaxReceiveCount });
        }
        await sqs.CreateQueueAsync(req);
    }

    public async Task DeleteQueueAsync(string url) => await sqs.DeleteQueueAsync(url);

    public async Task<List<SqsMessageInfo>> ReceiveMessagesAsync(ReceiveMessagesViewModel model)
    {
        var resp = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = model.QueueUrl,
            MaxNumberOfMessages = Math.Clamp(model.MaxMessages, 1, 10),
            WaitTimeSeconds = Math.Clamp(model.WaitTimeSeconds, 0, 20),
            AttributeNames = ["All"],
            MessageAttributeNames = ["All"]
        });
        return resp.Messages.Select(m =>
        {
            long ts = 0;
            m.Attributes.TryGetValue("SentTimestamp", out var tsStr);
            long.TryParse(tsStr, out ts);
            m.Attributes.TryGetValue("ApproximateReceiveCount", out var rcStr);
            int.TryParse(rcStr, out var rc);
            m.Attributes.TryGetValue("SenderId", out var senderId);
            return new SqsMessageInfo
            {
                MessageId = m.MessageId,
                ReceiptHandle = m.ReceiptHandle,
                Body = m.Body,
                SentTimestamp = ts > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime : null,
                SenderId = senderId,
                ApproximateReceiveCount = rc,
                QueueUrl = model.QueueUrl,
                QueueName = model.QueueName
            };
        }).ToList();
    }

    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle)
        => await sqs.DeleteMessageAsync(new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = receiptHandle });

    public async Task SendMessageAsync(SendMessageViewModel model)
    {
        var req = new SendMessageRequest { QueueUrl = model.QueueUrl, MessageBody = model.Body };
        if (!string.IsNullOrWhiteSpace(model.MessageGroupId))
            req.MessageGroupId = model.MessageGroupId;
        if (!string.IsNullOrWhiteSpace(model.MessageDeduplicationId))
            req.MessageDeduplicationId = model.MessageDeduplicationId;
        await sqs.SendMessageAsync(req);
    }

    // ─── SNS ───────────────────────────────────────────────────────────────────

    private async Task<int> GetSnsTopicCountAsync() => (await sns.ListTopicsAsync()).Topics.Count;

    public async Task<List<SnsTopicInfo>> ListTopicsAsync()
    {
        var resp = await sns.ListTopicsAsync();
        var result = new List<SnsTopicInfo>();
        foreach (var topic in resp.Topics)
        {
            try
            {
                var attrs = await sns.GetTopicAttributesAsync(topic.TopicArn);
                var a = attrs.Attributes;
                var info = new SnsTopicInfo
                {
                    Arn = topic.TopicArn, Name = topic.TopicArn.Split(':').Last(),
                    SubscriptionsCount = int.TryParse(a.GetValueOrDefault("SubscriptionsConfirmed"), out var sc) ? sc : 0,
                    IsFifo = topic.TopicArn.EndsWith(".fifo")
                };
                // Load subscriptions
                try
                {
                    var subs = await sns.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest { TopicArn = topic.TopicArn });
                    info.Subscriptions = subs.Subscriptions.Select(s => new SnsSubscriptionInfo
                    {
                        SubscriptionArn = s.SubscriptionArn, Protocol = s.Protocol, Endpoint = s.Endpoint
                    }).ToList();
                }
                catch { }
                result.Add(info);
            }
            catch { result.Add(new SnsTopicInfo { Arn = topic.TopicArn, Name = topic.TopicArn.Split(':').Last() }); }
        }
        return result;
    }

    public async Task CreateTopicAsync(CreateTopicViewModel model)
    {
        var req = new CreateTopicRequest { Name = model.Fifo && !model.TopicName.EndsWith(".fifo") ? model.TopicName + ".fifo" : model.TopicName };
        if (model.Fifo)
        {
            req.Attributes["FifoTopic"] = "true";
            if (model.ContentBasedDeduplication)
                req.Attributes["ContentBasedDeduplication"] = "true";
        }
        await sns.CreateTopicAsync(req);
    }

    public async Task DeleteTopicAsync(string arn) => await sns.DeleteTopicAsync(arn);

    public async Task PublishAsync(PublishViewModel model)
    {
        var req = new PublishRequest { TopicArn = model.TopicArn, Message = model.Message, Subject = model.Subject };
        if (!string.IsNullOrWhiteSpace(model.MessageAttributes))
        {
            try
            {
                var attrs = JsonSerializer.Deserialize<Dictionary<string, string>>(model.MessageAttributes);
                if (attrs != null)
                    foreach (var kv in attrs)
                        req.MessageAttributes[kv.Key] = new SnsMessageAttributeValue { DataType = "String", StringValue = kv.Value };
            }
            catch { }
        }
        await sns.PublishAsync(req);
    }

    public async Task SubscribeAsync(SnsSubscribeViewModel model)
    {
        var req = new SubscribeRequest { TopicArn = model.TopicArn, Protocol = model.Protocol, Endpoint = model.Endpoint };
        if (!string.IsNullOrWhiteSpace(model.FilterPolicy))
            req.Attributes["FilterPolicy"] = model.FilterPolicy;
        await sns.SubscribeAsync(req);
    }

    public async Task UnsubscribeAsync(string subscriptionArn) => await sns.UnsubscribeAsync(subscriptionArn);

    // ─── Lambda ────────────────────────────────────────────────────────────────

    private async Task<int> GetLambdaFunctionCountAsync() => (await lambda.ListFunctionsAsync(new ListFunctionsRequest())).Functions.Count;

    public async Task<List<LambdaFunctionInfo>> ListFunctionsAsync()
    {
        var resp = await lambda.ListFunctionsAsync(new ListFunctionsRequest());
        return resp.Functions.Select(f => new LambdaFunctionInfo
        {
            Name = f.FunctionName, Arn = f.FunctionArn, Runtime = f.Runtime?.Value ?? "unknown",
            Handler = f.Handler, MemorySize = f.MemorySize, Timeout = f.Timeout,
            LastModified = f.LastModified, State = f.State?.Value ?? "Active", Description = f.Description
        }).ToList();
    }

    public async Task<string> InvokeFunctionAsync(string name, string payload)
    {
        var resp = await lambda.InvokeAsync(new InvokeRequest { FunctionName = name, Payload = payload, InvocationType = LambdaInvocationType.RequestResponse });
        using var reader = new StreamReader(resp.Payload);
        return await reader.ReadToEndAsync();
    }

    public async Task DeleteFunctionAsync(string name) => await lambda.DeleteFunctionAsync(name);

    // ─── KMS ───────────────────────────────────────────────────────────────────

    private async Task<int> GetKmsKeyCountAsync() => (await kms.ListKeysAsync(new ListKeysRequest())).Keys.Count;

    public async Task<List<KmsKeyInfo>> ListKeysAsync()
    {
        var resp = await kms.ListKeysAsync(new ListKeysRequest());
        var result = new List<KmsKeyInfo>();
        foreach (var key in resp.Keys)
        {
            try
            {
                var desc = await kms.DescribeKeyAsync(new DescribeKeyRequest { KeyId = key.KeyId });
                var m = desc.KeyMetadata;
                result.Add(new KmsKeyInfo { KeyId = m.KeyId, Arn = m.Arn, Description = m.Description, KeyState = m.KeyState.Value, KeyUsage = m.KeyUsage.Value, CreationDate = m.CreationDate });
            }
            catch { result.Add(new KmsKeyInfo { KeyId = key.KeyId, Arn = key.KeyArn }); }
        }
        return result;
    }

    public async Task CreateKeyAsync(string description) => await kms.CreateKeyAsync(new CreateKeyRequest { Description = description });

    // ─── IAM ───────────────────────────────────────────────────────────────────

    private async Task<int> GetIamUserCountAsync() => (await iam.ListUsersAsync(new IamListUsersRequest())).Users.Count;

    public async Task<List<IamUserInfo>> ListUsersAsync()
    {
        var resp = await iam.ListUsersAsync(new IamListUsersRequest());
        return resp.Users.Select(u => new IamUserInfo { UserId = u.UserId, UserName = u.UserName, Arn = u.Arn, CreateDate = u.CreateDate, Path = u.Path }).ToList();
    }

    public async Task CreateUserAsync(string userName, string path = "/")
        => await iam.CreateUserAsync(new IamCreateUserRequest { UserName = userName, Path = path });

    public async Task DeleteUserAsync(string userName) => await iam.DeleteUserAsync(new IamDeleteUserRequest { UserName = userName });

    public async Task<List<IamRoleInfo>> ListRolesAsync()
    {
        var resp = await iam.ListRolesAsync(new ListRolesRequest());
        return resp.Roles.Select(r => new IamRoleInfo { RoleId = r.RoleId, RoleName = r.RoleName, Arn = r.Arn, CreateDate = r.CreateDate, AssumeRolePolicyDocument = Uri.UnescapeDataString(r.AssumeRolePolicyDocument ?? "") }).ToList();
    }

    public async Task CreateRoleAsync(string roleName, string assumeRolePolicyDocument)
        => await iam.CreateRoleAsync(new CreateRoleRequest { RoleName = roleName, AssumeRolePolicyDocument = assumeRolePolicyDocument });

    public async Task DeleteRoleAsync(string roleName) => await iam.DeleteRoleAsync(new DeleteRoleRequest { RoleName = roleName });

    // ─── SSM ───────────────────────────────────────────────────────────────────

    private async Task<int> GetSsmParameterCountAsync() => (await ssm.DescribeParametersAsync(new DescribeParametersRequest())).Parameters.Count;

    public async Task<List<SsmParameterInfo>> ListParametersAsync()
    {
        var resp = await ssm.DescribeParametersAsync(new DescribeParametersRequest());
        return resp.Parameters.Select(p => new SsmParameterInfo { Name = p.Name, Type = p.Type.Value, LastModifiedDate = p.LastModifiedDate, Description = p.Description, Version = p.Version }).ToList();
    }

    public async Task PutParameterAsync(PutParameterViewModel model)
        => await ssm.PutParameterAsync(new PutParameterRequest
        {
            Name = model.Name, Value = model.Value,
            Type = new ParameterType(model.Type),
            Description = model.Description, Overwrite = model.Overwrite
        });

    public async Task DeleteParameterAsync(string name) => await ssm.DeleteParameterAsync(new DeleteParameterRequest { Name = name });

    // ─── Secrets Manager ───────────────────────────────────────────────────────

    private async Task<int> GetSecretsCountAsync() => (await secrets.ListSecretsAsync(new ListSecretsRequest())).SecretList.Count;

    public async Task<List<SecretInfo>> ListSecretsAsync()
    {
        var resp = await secrets.ListSecretsAsync(new ListSecretsRequest());
        return resp.SecretList.Select(s => new SecretInfo { Name = s.Name, Arn = s.ARN, LastChangedDate = s.LastChangedDate, Description = s.Description, KmsKeyId = s.KmsKeyId }).ToList();
    }

    public async Task CreateSecretAsync(CreateSecretViewModel model)
        => await secrets.CreateSecretAsync(new CreateSecretRequest { Name = model.Name, SecretString = model.SecretString, Description = model.Description, KmsKeyId = string.IsNullOrWhiteSpace(model.KmsKeyId) ? null : model.KmsKeyId });

    public async Task DeleteSecretAsync(string name, bool forceDelete = true)
        => await secrets.DeleteSecretAsync(new DeleteSecretRequest { SecretId = name, ForceDeleteWithoutRecovery = forceDelete });

    public async Task<string> GetSecretValueAsync(string name)
    {
        var resp = await secrets.GetSecretValueAsync(new GetSecretValueRequest { SecretId = name });
        return resp.SecretString ?? "[binary secret]";
    }

    // ─── SES ───────────────────────────────────────────────────────────────────

    public async Task<List<SesIdentityInfo>> ListIdentitiesAsync()
    {
        var resp = await ses.ListIdentitiesAsync(new ListIdentitiesRequest());
        var result = new List<SesIdentityInfo>();
        if (resp.Identities.Count > 0)
        {
            try
            {
                var verification = await ses.GetIdentityVerificationAttributesAsync(new GetIdentityVerificationAttributesRequest { Identities = resp.Identities });
                foreach (var id in resp.Identities)
                {
                    var status = verification.VerificationAttributes.TryGetValue(id, out var attr) ? attr.VerificationStatus.Value : "Unknown";
                    result.Add(new SesIdentityInfo { Identity = id, VerificationStatus = status, IsEmail = id.Contains('@') });
                }
            }
            catch { result.AddRange(resp.Identities.Select(id => new SesIdentityInfo { Identity = id, IsEmail = id.Contains('@') })); }
        }
        return result;
    }

    public async Task VerifyEmailAsync(string email) => await ses.VerifyEmailIdentityAsync(new VerifyEmailIdentityRequest { EmailAddress = email });

    public async Task DeleteIdentityAsync(string identity) => await ses.DeleteIdentityAsync(new DeleteIdentityRequest { Identity = identity });

    public async Task SendEmailAsync(SesSendEmailViewModel model)
        => await ses.SendEmailAsync(new SendEmailRequest
        {
            Source = model.From,
            Destination = new SesDestination { ToAddresses = [model.To] },
            Message = new Amazon.SimpleEmail.Model.Message
            {
                Subject = new Content { Data = model.Subject },
                Body = model.IsHtml
                    ? new Body { Html = new Content { Data = model.Body } }
                    : new Body { Text = new Content { Data = model.Body } }
            }
        });

    // ─── Cognito ───────────────────────────────────────────────────────────────

    private async Task<int> GetCognitoUserPoolCountAsync() => (await cognito.ListUserPoolsAsync(new ListUserPoolsRequest { MaxResults = 60 })).UserPools.Count;

    public async Task<List<CognitoUserPoolInfo>> ListUserPoolsAsync()
    {
        var resp = await cognito.ListUserPoolsAsync(new ListUserPoolsRequest { MaxResults = 60 });
        var result = new List<CognitoUserPoolInfo>();
        foreach (var p in resp.UserPools)
        {
            int userCount = 0;
            try { userCount = (await cognito.ListUsersAsync(new Amazon.CognitoIdentityProvider.Model.ListUsersRequest { UserPoolId = p.Id })).Users.Count; } catch { }
            result.Add(new CognitoUserPoolInfo { Id = p.Id, Name = p.Name, CreationDate = p.CreationDate, Status = p.Status?.Value ?? "", UserCount = userCount });
        }
        return result;
    }

    public async Task<List<CognitoUserInfo>> ListUsersInPoolAsync(string userPoolId)
    {
        var resp = await cognito.ListUsersAsync(new Amazon.CognitoIdentityProvider.Model.ListUsersRequest { UserPoolId = userPoolId });
        return resp.Users.Select(u => new CognitoUserInfo
        {
            Username = u.Username, UserStatus = u.UserStatus.Value, UserCreateDate = u.UserCreateDate,
            Attributes = u.Attributes.Select(a => $"{a.Name}={a.Value}").ToList()
        }).ToList();
    }

    public async Task CreateUserPoolAsync(CreateUserPoolViewModel model)
    {
        var req = new CreateUserPoolRequest { PoolName = model.PoolName };
        if (model.UsernameAsEmail) req.UsernameAttributes = ["email"];
        if (model.MfaEnabled) req.MfaConfiguration = UserPoolMfaType.ON;
        await cognito.CreateUserPoolAsync(req);
    }

    public async Task DeleteUserPoolAsync(string userPoolId) => await cognito.DeleteUserPoolAsync(new DeleteUserPoolRequest { UserPoolId = userPoolId });

    public async Task CreateCognitoUserAsync(CreateCognitoUserViewModel model)
        => await cognito.AdminCreateUserAsync(new AdminCreateUserRequest
        {
            UserPoolId = model.UserPoolId, Username = model.Username, TemporaryPassword = model.TemporaryPassword,
            UserAttributes = string.IsNullOrWhiteSpace(model.Email) ? [] : [new AttributeType { Name = "email", Value = model.Email }]
        });

    // ─── Kinesis ───────────────────────────────────────────────────────────────

    private async Task<int> GetKinesisStreamCountAsync() => (await kinesis.ListStreamsAsync(new KinesisListStreamsRequest())).StreamNames.Count;

    public async Task<List<KinesisStreamInfo>> ListStreamsAsync()
    {
        var resp = await kinesis.ListStreamsAsync(new KinesisListStreamsRequest());
        var result = new List<KinesisStreamInfo>();
        foreach (var name in resp.StreamNames)
        {
            try
            {
                var desc = await kinesis.DescribeStreamSummaryAsync(new DescribeStreamSummaryRequest { StreamName = name });
                var s = desc.StreamDescriptionSummary;
                result.Add(new KinesisStreamInfo
                {
                    Name = s.StreamName, Arn = s.StreamARN, Status = s.StreamStatus.Value,
                    ShardCount = s.OpenShardCount, CreationTimestamp = s.StreamCreationTimestamp,
                    StreamMode = s.StreamModeDetails?.StreamMode?.Value ?? "PROVISIONED"
                });
            }
            catch { result.Add(new KinesisStreamInfo { Name = name }); }
        }
        return result;
    }

    public async Task CreateStreamAsync(CreateKinesisStreamViewModel model)
    {
        var req = new CreateStreamRequest { StreamName = model.StreamName, ShardCount = model.ShardCount };
        if (model.StreamMode == "ON_DEMAND") req.StreamModeDetails = new StreamModeDetails { StreamMode = StreamMode.ON_DEMAND };
        await kinesis.CreateStreamAsync(req);
    }

    public async Task DeleteStreamAsync(string name) => await kinesis.DeleteStreamAsync(new DeleteStreamRequest { StreamName = name });

    public async Task PutKinesisRecordAsync(KinesisPutRecordViewModel model)
        => await kinesis.PutRecordAsync(new KinesisPutRecordRequest { StreamName = model.StreamName, Data = new MemoryStream(Encoding.UTF8.GetBytes(model.Data)), PartitionKey = model.PartitionKey });

    // ─── Firehose ──────────────────────────────────────────────────────────────

    public async Task<List<FirehoseDeliveryStreamInfo>> ListDeliveryStreamsAsync()
    {
        var resp = await firehose.ListDeliveryStreamsAsync(new ListDeliveryStreamsRequest());
        var result = new List<FirehoseDeliveryStreamInfo>();
        foreach (var name in resp.DeliveryStreamNames)
        {
            try
            {
                var desc = await firehose.DescribeDeliveryStreamAsync(new DescribeDeliveryStreamRequest { DeliveryStreamName = name });
                var s = desc.DeliveryStreamDescription;
                result.Add(new FirehoseDeliveryStreamInfo { Name = s.DeliveryStreamName, Arn = s.DeliveryStreamARN, Status = s.DeliveryStreamStatus.Value, DeliveryStreamType = s.DeliveryStreamType.Value, CreateTimestamp = s.CreateTimestamp });
            }
            catch { result.Add(new FirehoseDeliveryStreamInfo { Name = name }); }
        }
        return result;
    }

    public async Task CreateDeliveryStreamAsync(CreateFirehoseStreamViewModel model)
    {
        var req = new CreateDeliveryStreamRequest
        {
            DeliveryStreamName = model.DeliveryStreamName,
            DeliveryStreamType = new DeliveryStreamType(model.DeliveryStreamType)
        };
        if (model.DestinationType == "S3")
            req.S3DestinationConfiguration = new S3DestinationConfiguration { BucketARN = model.S3BucketArn, RoleARN = "arn:aws:iam::000000000000:role/firehose-role", BufferingHints = new BufferingHints { IntervalInSeconds = 60, SizeInMBs = 1 } };
        await firehose.CreateDeliveryStreamAsync(req);
    }

    public async Task DeleteDeliveryStreamAsync(string name) => await firehose.DeleteDeliveryStreamAsync(new DeleteDeliveryStreamRequest { DeliveryStreamName = name });

    // ─── Step Functions ────────────────────────────────────────────────────────

    private async Task<int> GetStateMachineCountAsync() => (await sfn.ListStateMachinesAsync(new ListStateMachinesRequest())).StateMachines.Count;

    public async Task<List<StateMachineInfo>> ListStateMachinesAsync()
    {
        var resp = await sfn.ListStateMachinesAsync(new ListStateMachinesRequest());
        return resp.StateMachines.Select(s => new StateMachineInfo { Name = s.Name, Arn = s.StateMachineArn, Status = "ACTIVE", Type = s.Type?.Value ?? "STANDARD", CreationDate = s.CreationDate }).ToList();
    }

    public async Task CreateStateMachineAsync(CreateStateMachineViewModel model)
        => await sfn.CreateStateMachineAsync(new CreateStateMachineRequest { Name = model.Name, Definition = model.Definition, Type = new StateMachineType(model.Type), RoleArn = string.IsNullOrWhiteSpace(model.RoleArn) ? "arn:aws:iam::000000000000:role/sfn-role" : model.RoleArn });

    public async Task DeleteStateMachineAsync(string arn) => await sfn.DeleteStateMachineAsync(new DeleteStateMachineRequest { StateMachineArn = arn });

    public async Task<string> StartExecutionAsync(StartExecutionViewModel model)
    {
        var resp = await sfn.StartExecutionAsync(new StartExecutionRequest { StateMachineArn = model.StateMachineArn, Input = model.Input, Name = string.IsNullOrWhiteSpace(model.ExecutionName) ? null : model.ExecutionName });
        return resp.ExecutionArn;
    }

    public async Task<List<StateMachineExecutionInfo>> ListExecutionsAsync(string stateMachineArn)
    {
        var resp = await sfn.ListExecutionsAsync(new SfnListExecutionsRequest { StateMachineArn = stateMachineArn });
        return resp.Executions.Select(e => new StateMachineExecutionInfo { ExecutionArn = e.ExecutionArn, Name = e.Name, Status = e.Status.Value, StartDate = e.StartDate, StopDate = e.StopDate == default ? null : e.StopDate }).ToList();
    }

    // ─── CloudFormation ────────────────────────────────────────────────────────

    public async Task<List<CloudFormationStackInfo>> ListStacksAsync()
    {
        var resp = await cfn.ListStacksAsync(new ListStacksRequest());
        var result = new List<CloudFormationStackInfo>();
        foreach (var s in resp.StackSummaries.Where(s => s.StackStatus != StackStatus.DELETE_COMPLETE))
        {
            result.Add(new CloudFormationStackInfo { StackId = s.StackId, StackName = s.StackName, Status = s.StackStatus.Value, CreationTime = s.CreationTime, Description = s.TemplateDescription ?? "" });
        }
        return result;
    }

    public async Task CreateStackAsync(CreateStackViewModel model)
        => await cfn.CreateStackAsync(new CreateStackRequest { StackName = model.StackName, TemplateBody = model.TemplateBody });

    public async Task DeleteStackAsync(string name) => await cfn.DeleteStackAsync(new DeleteStackRequest { StackName = name });

    // ─── EventBridge ───────────────────────────────────────────────────────────

    private async Task<int> GetEventBridgeBusCountAsync() => (await eventBridge.ListEventBusesAsync(new ListEventBusesRequest())).EventBuses.Count;

    public async Task<List<EventBridgeBusInfo>> ListEventBusesAsync()
    {
        var resp = await eventBridge.ListEventBusesAsync(new ListEventBusesRequest());
        return resp.EventBuses.Select(b => new EventBridgeBusInfo { Name = b.Name, Arn = b.Arn }).ToList();
    }

    public async Task CreateEventBusAsync(string name) => await eventBridge.CreateEventBusAsync(new CreateEventBusRequest { Name = name });
    public async Task DeleteEventBusAsync(string name) => await eventBridge.DeleteEventBusAsync(new DeleteEventBusRequest { Name = name });

    public async Task<List<EventBridgeRuleInfo>> ListRulesAsync(string eventBusName = "default")
    {
        var resp = await eventBridge.ListRulesAsync(new ListRulesRequest { EventBusName = eventBusName });
        return resp.Rules.Select(r => new EventBridgeRuleInfo { Name = r.Name, Arn = r.Arn, State = r.State.Value, EventPattern = r.EventPattern ?? "", ScheduleExpression = r.ScheduleExpression ?? "", EventBusName = r.EventBusName ?? "default" }).ToList();
    }

    public async Task CreateRuleAsync(CreateEventRuleViewModel model)
    {
        var req = new PutRuleRequest { Name = model.Name, EventBusName = model.EventBusName, State = new RuleState(model.State) };
        if (!string.IsNullOrWhiteSpace(model.EventPattern)) req.EventPattern = model.EventPattern;
        if (!string.IsNullOrWhiteSpace(model.ScheduleExpression)) req.ScheduleExpression = model.ScheduleExpression;
        await eventBridge.PutRuleAsync(req);
    }

    public async Task DeleteRuleAsync(string name, string eventBusName) => await eventBridge.DeleteRuleAsync(new EventBridgeDeleteRuleRequest { Name = name, EventBusName = eventBusName });

    public async Task PutEventAsync(PutEventViewModel model)
        => await eventBridge.PutEventsAsync(new PutEventsRequest { Entries = [new PutEventsRequestEntry { EventBusName = model.EventBusName, Source = model.Source, DetailType = model.DetailType, Detail = model.Detail }] });

    // ─── EventBridge Scheduler ─────────────────────────────────────────────────

    public async Task<List<SchedulerScheduleInfo>> ListSchedulesAsync()
    {
        var resp = await scheduler.ListSchedulesAsync(new ListSchedulesRequest());
        return resp.Schedules.Select(s => new SchedulerScheduleInfo { Name = s.Name, Arn = s.Arn, State = s.State?.Value ?? "", ScheduleExpression = "", GroupName = s.GroupName }).ToList();
    }

    public async Task CreateScheduleAsync(CreateScheduleViewModel model)
        => await scheduler.CreateScheduleAsync(new CreateScheduleRequest
        {
            Name = model.Name, GroupName = model.GroupName, ScheduleExpression = model.ScheduleExpression,
            State = new SchedulerScheduleState(model.State),
            Target = new Amazon.Scheduler.Model.Target { Arn = model.TargetArn, RoleArn = model.TargetRoleArn, Input = model.Input },
            FlexibleTimeWindow = new FlexibleTimeWindow { Mode = FlexibleTimeWindowMode.OFF }
        });

    public async Task DeleteScheduleAsync(string name, string groupName) => await scheduler.DeleteScheduleAsync(new DeleteScheduleRequest { Name = name, GroupName = groupName });

    // ─── CloudWatch ────────────────────────────────────────────────────────────

    public async Task<List<CloudWatchAlarmInfo>> ListAlarmsAsync()
    {
        var resp = await cloudWatch.DescribeAlarmsAsync(new DescribeAlarmsRequest());
        return resp.MetricAlarms.Select(a => new CloudWatchAlarmInfo { AlarmName = a.AlarmName, AlarmArn = a.AlarmArn, StateValue = a.StateValue.Value, MetricName = a.MetricName, Namespace = a.Namespace, ComparisonOperator = a.ComparisonOperator.Value, Threshold = a.Threshold }).ToList();
    }

    public async Task<List<CloudWatchLogGroupInfo>> ListLogGroupsAsync()
    {
        var resp = await cloudWatchLogs.DescribeLogGroupsAsync(new DescribeLogGroupsRequest());
        return resp.LogGroups.Select(g => new CloudWatchLogGroupInfo { LogGroupName = g.LogGroupName, StoredBytes = g.StoredBytes, RetentionInDays = g.RetentionInDays, CreationTime = g.CreationTime == default ? null : g.CreationTime }).ToList();
    }

    public async Task CreateLogGroupAsync(CreateLogGroupViewModel model)
    {
        await cloudWatchLogs.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = model.LogGroupName });
        if (model.RetentionInDays.HasValue)
            await cloudWatchLogs.PutRetentionPolicyAsync(new PutRetentionPolicyRequest { LogGroupName = model.LogGroupName, RetentionInDays = model.RetentionInDays.Value });
    }

    public async Task DeleteLogGroupAsync(string name) => await cloudWatchLogs.DeleteLogGroupAsync(new DeleteLogGroupRequest { LogGroupName = name });

    // ─── ElastiCache ───────────────────────────────────────────────────────────

    private async Task<int> GetElastiCacheClusterCountAsync() => (await elastiCache.DescribeCacheClustersAsync(new DescribeCacheClustersRequest())).CacheClusters.Count;

    public async Task<List<ElastiCacheClusterInfo>> ListCacheClustersAsync()
    {
        var resp = await elastiCache.DescribeCacheClustersAsync(new DescribeCacheClustersRequest());
        return resp.CacheClusters.Select(c => new ElastiCacheClusterInfo { ClusterId = c.CacheClusterId, Status = c.CacheClusterStatus, Engine = c.Engine, EngineVersion = c.EngineVersion, CacheNodeType = c.CacheNodeType, NumCacheNodes = c.NumCacheNodes }).ToList();
    }

    public async Task CreateCacheClusterAsync(CreateElastiCacheClusterViewModel model)
        => await elastiCache.CreateCacheClusterAsync(new CreateCacheClusterRequest { CacheClusterId = model.ClusterId, Engine = model.Engine, EngineVersion = model.EngineVersion, CacheNodeType = model.CacheNodeType, NumCacheNodes = model.NumCacheNodes });

    public async Task DeleteCacheClusterAsync(string clusterId) => await elastiCache.DeleteCacheClusterAsync(new DeleteCacheClusterRequest { CacheClusterId = clusterId });

    // ─── RDS ───────────────────────────────────────────────────────────────────

    private async Task<int> GetRdsInstanceCountAsync() => (await rds.DescribeDBInstancesAsync(new DescribeDBInstancesRequest())).DBInstances.Count;

    public async Task<List<RdsInstanceInfo>> ListDbInstancesAsync()
    {
        var resp = await rds.DescribeDBInstancesAsync(new DescribeDBInstancesRequest());
        return resp.DBInstances.Select(d => new RdsInstanceInfo { DBInstanceIdentifier = d.DBInstanceIdentifier, DBInstanceStatus = d.DBInstanceStatus, Engine = d.Engine, EngineVersion = d.EngineVersion, DBInstanceClass = d.DBInstanceClass, Endpoint = d.Endpoint?.Address ?? "", Port = d.Endpoint?.Port ?? 0, MasterUsername = d.MasterUsername }).ToList();
    }

    public async Task CreateDbInstanceAsync(CreateRdsInstanceViewModel model)
        => await rds.CreateDBInstanceAsync(new CreateDBInstanceRequest { DBInstanceIdentifier = model.DBInstanceIdentifier, Engine = model.Engine, EngineVersion = model.EngineVersion, DBInstanceClass = model.DBInstanceClass, MasterUsername = model.MasterUsername, MasterUserPassword = model.MasterUserPassword, AllocatedStorage = model.AllocatedStorage, DBName = string.IsNullOrWhiteSpace(model.DBName) ? null : model.DBName });

    public async Task DeleteDbInstanceAsync(string id) => await rds.DeleteDBInstanceAsync(new DeleteDBInstanceRequest { DBInstanceIdentifier = id, SkipFinalSnapshot = true });

    // ─── Glue ──────────────────────────────────────────────────────────────────

    public async Task<List<GlueDatabaseInfo>> ListGlueDatabasesAsync()
    {
        var resp = await glue.GetDatabasesAsync(new GetDatabasesRequest());
        var result = new List<GlueDatabaseInfo>();
        foreach (var db in resp.DatabaseList)
        {
            int tableCount = 0;
            try { tableCount = (await glue.GetTablesAsync(new GetTablesRequest { DatabaseName = db.Name })).TableList.Count; } catch { }
            result.Add(new GlueDatabaseInfo { Name = db.Name, CreateTime = db.CreateTime, Description = db.Description, TableCount = tableCount });
        }
        return result;
    }

    public async Task<List<GlueTableInfo>> ListGlueTablesAsync(string databaseName)
    {
        var resp = await glue.GetTablesAsync(new GetTablesRequest { DatabaseName = databaseName });
        return resp.TableList.Select(t => new GlueTableInfo { Name = t.Name, DatabaseName = t.DatabaseName, CreateTime = t.CreateTime, TableType = t.TableType }).ToList();
    }

    public async Task CreateGlueDatabaseAsync(CreateGlueDatabaseViewModel model)
        => await glue.CreateDatabaseAsync(new CreateDatabaseRequest { DatabaseInput = new DatabaseInput { Name = model.Name, Description = model.Description } });

    public async Task DeleteGlueDatabaseAsync(string name) => await glue.DeleteDatabaseAsync(new DeleteDatabaseRequest { Name = name });

    // ─── Athena ────────────────────────────────────────────────────────────────

    public async Task<List<AthenaWorkGroupInfo>> ListWorkGroupsAsync()
    {
        var resp = await athena.ListWorkGroupsAsync(new ListWorkGroupsRequest());
        return resp.WorkGroups.Select(w => new AthenaWorkGroupInfo { Name = w.Name, State = w.State?.Value ?? "", Description = w.Description }).ToList();
    }

    public async Task<string> StartQueryExecutionAsync(AthenaQueryViewModel model)
    {
        var resp = await athena.StartQueryExecutionAsync(new StartQueryExecutionRequest
        {
            QueryString = model.QueryString, WorkGroup = model.WorkGroup,
            ResultConfiguration = string.IsNullOrWhiteSpace(model.OutputLocation) ? null : new ResultConfiguration { OutputLocation = model.OutputLocation }
        });
        return resp.QueryExecutionId;
    }

    // ─── ECS ───────────────────────────────────────────────────────────────────

    private async Task<int> GetEcsClusterCountAsync() => (await ecs.ListClustersAsync(new ListClustersRequest())).ClusterArns.Count;

    public async Task<List<EcsClusterInfo>> ListEcsClustersAsync()
    {
        var arns = (await ecs.ListClustersAsync(new ListClustersRequest())).ClusterArns;
        if (arns.Count == 0) return [];
        var resp = await ecs.DescribeClustersAsync(new DescribeClustersRequest { Clusters = arns });
        return resp.Clusters.Select(c => new EcsClusterInfo { ClusterArn = c.ClusterArn, ClusterName = c.ClusterName, Status = c.Status, RunningTasksCount = c.RunningTasksCount, ActiveServicesCount = c.ActiveServicesCount, RegisteredContainerInstancesCount = c.RegisteredContainerInstancesCount }).ToList();
    }

    public async Task<List<EcsTaskDefinitionInfo>> ListTaskDefinitionsAsync()
    {
        var arns = (await ecs.ListTaskDefinitionsAsync(new ListTaskDefinitionsRequest())).TaskDefinitionArns;
        var result = new List<EcsTaskDefinitionInfo>();
        foreach (var arn in arns.Take(50))
        {
            try
            {
                var desc = await ecs.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest { TaskDefinition = arn });
                var td = desc.TaskDefinition;
                result.Add(new EcsTaskDefinitionInfo { TaskDefinitionArn = td.TaskDefinitionArn, Family = td.Family, Status = td.Status.Value, Revision = td.Revision, NetworkMode = td.NetworkMode?.Value ?? "", ContainerNames = td.ContainerDefinitions.Select(c => c.Name).ToList() });
            }
            catch { }
        }
        return result;
    }

    public async Task CreateEcsClusterAsync(string name) => await ecs.CreateClusterAsync(new CreateClusterRequest { ClusterName = name });
    public async Task DeleteEcsClusterAsync(string arn) => await ecs.DeleteClusterAsync(new DeleteClusterRequest { Cluster = arn });

    public async Task RegisterTaskDefinitionAsync(RegisterTaskDefinitionViewModel model)
        => await ecs.RegisterTaskDefinitionAsync(new RegisterTaskDefinitionRequest
        {
            Family = model.Family, NetworkMode = new NetworkMode(model.NetworkMode),
            RequiresCompatibilities = ["FARGATE"],
            Cpu = model.Cpu.ToString(), Memory = model.Memory.ToString(),
            ExecutionRoleArn = string.IsNullOrWhiteSpace(model.ExecutionRoleArn) ? null : model.ExecutionRoleArn,
            ContainerDefinitions = [new ContainerDefinition { Name = model.ContainerName, Image = model.Image, Cpu = model.Cpu, Memory = model.Memory, PortMappings = [new PortMapping { ContainerPort = model.ContainerPort, Protocol = TransportProtocol.Tcp }] }]
        });

    public async Task RunEcsTaskAsync(RunEcsTaskViewModel model)
        => await ecs.RunTaskAsync(new RunTaskRequest { Cluster = model.ClusterArn, TaskDefinition = model.TaskDefinition, Count = model.Count, LaunchType = new EcsLaunchType(model.LaunchType) });

    // ─── ECR ───────────────────────────────────────────────────────────────────

    private async Task<int> GetEcrRepositoryCountAsync() => (await ecr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest())).Repositories.Count;

    public async Task<List<EcrRepositoryInfo>> ListRepositoriesAsync()
    {
        var resp = await ecr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest());
        var result = new List<EcrRepositoryInfo>();
        foreach (var r in resp.Repositories)
        {
            int imageCount = 0;
            try { imageCount = (await ecr.DescribeImagesAsync(new DescribeImagesRequest { RepositoryName = r.RepositoryName })).ImageDetails.Count; } catch { }
            result.Add(new EcrRepositoryInfo { RepositoryArn = r.RepositoryArn, RepositoryName = r.RepositoryName, RepositoryUri = r.RepositoryUri, CreatedAt = r.CreatedAt, ImageTagMutability = r.ImageTagMutability.Value, ImageCount = imageCount });
        }
        return result;
    }

    public async Task CreateRepositoryAsync(CreateEcrRepositoryViewModel model)
        => await ecr.CreateRepositoryAsync(new CreateRepositoryRequest { RepositoryName = model.RepositoryName, ImageTagMutability = new ImageTagMutability(model.ImageTagMutability), ImageScanningConfiguration = new ImageScanningConfiguration { ScanOnPush = model.ScanOnPush } });

    public async Task DeleteRepositoryAsync(string name) => await ecr.DeleteRepositoryAsync(new DeleteRepositoryRequest { RepositoryName = name, Force = true });

    // ─── ELB v2 ────────────────────────────────────────────────────────────────

    public async Task<List<LoadBalancerInfo>> ListLoadBalancersAsync()
    {
        var resp = await elb.DescribeLoadBalancersAsync(new ElbDescribeLoadBalancersRequest());
        return resp.LoadBalancers.Select(lb => new LoadBalancerInfo { LoadBalancerArn = lb.LoadBalancerArn, LoadBalancerName = lb.LoadBalancerName, Type = lb.Type.Value, Scheme = lb.Scheme.Value, State = lb.State?.Code?.Value ?? "", DNSName = lb.DNSName, CreatedTime = lb.CreatedTime }).ToList();
    }

    public async Task<List<TargetGroupInfo>> ListTargetGroupsAsync()
    {
        var resp = await elb.DescribeTargetGroupsAsync(new DescribeTargetGroupsRequest());
        return resp.TargetGroups.Select(tg => new TargetGroupInfo { TargetGroupArn = tg.TargetGroupArn, TargetGroupName = tg.TargetGroupName, Protocol = tg.Protocol?.Value ?? "", Port = tg?.Port ?? 0, TargetType = tg.TargetType?.Value ?? "", VpcId = tg.VpcId ?? "" }).ToList();
    }

    public async Task CreateTargetGroupAsync(CreateTargetGroupViewModel model)
        => await elb.CreateTargetGroupAsync(new CreateTargetGroupRequest { Name = model.Name, Protocol = new ProtocolEnum(model.Protocol), Port = model.Port, TargetType = new TargetTypeEnum(model.TargetType), VpcId = string.IsNullOrWhiteSpace(model.VpcId) ? null : model.VpcId });

    public async Task DeleteLoadBalancerAsync(string arn) => await elb.DeleteLoadBalancerAsync(new DeleteLoadBalancerRequest { LoadBalancerArn = arn });
    public async Task DeleteTargetGroupAsync(string arn) => await elb.DeleteTargetGroupAsync(new DeleteTargetGroupRequest { TargetGroupArn = arn });

    // ─── Auto Scaling ──────────────────────────────────────────────────────────

    public async Task<List<AutoScalingGroupInfo>> ListAutoScalingGroupsAsync()
    {
        var resp = await autoScaling.DescribeAutoScalingGroupsAsync(new DescribeAutoScalingGroupsRequest());
        return resp.AutoScalingGroups.Select(g => new AutoScalingGroupInfo { AutoScalingGroupName = g.AutoScalingGroupName, AutoScalingGroupArn = g.AutoScalingGroupARN, MinSize = g.MinSize, MaxSize = g.MaxSize, DesiredCapacity = g.DesiredCapacity, Status = g.Status ?? "Active", AvailabilityZones = g.AvailabilityZones }).ToList();
    }

    public async Task DeleteAutoScalingGroupAsync(string name) => await autoScaling.DeleteAutoScalingGroupAsync(new DeleteAutoScalingGroupRequest { AutoScalingGroupName = name, ForceDelete = true });

    // ─── Backup ────────────────────────────────────────────────────────────────

    public async Task<List<BackupVaultInfo>> ListBackupVaultsAsync()
    {
        var resp = await backup.ListBackupVaultsAsync(new ListBackupVaultsRequest());
        return resp.BackupVaultList.Select(v => new BackupVaultInfo { BackupVaultName = v.BackupVaultName, BackupVaultArn = v.BackupVaultArn, CreationDate = v.CreationDate, NumberOfRecoveryPoints = (int)v.NumberOfRecoveryPoints, EncryptionKeyArn = v.EncryptionKeyArn ?? "" }).ToList();
    }

    public async Task<List<BackupPlanInfo>> ListBackupPlansAsync()
    {
        var resp = await backup.ListBackupPlansAsync(new ListBackupPlansRequest());
        return resp.BackupPlansList.Select(p => new BackupPlanInfo { BackupPlanId = p.BackupPlanId, BackupPlanName = p.BackupPlanName, BackupPlanArn = p.BackupPlanArn, CreationDate = p.CreationDate }).ToList();
    }

    public async Task CreateBackupVaultAsync(CreateBackupVaultViewModel model)
        => await backup.CreateBackupVaultAsync(new CreateBackupVaultRequest { BackupVaultName = model.BackupVaultName, EncryptionKeyArn = string.IsNullOrWhiteSpace(model.EncryptionKeyArn) ? null : model.EncryptionKeyArn });

    public async Task DeleteBackupVaultAsync(string name) => await backup.DeleteBackupVaultAsync(new DeleteBackupVaultRequest { BackupVaultName = name });

    public async Task CreateBackupPlanAsync(CreateBackupPlanViewModel model)
        => await backup.CreateBackupPlanAsync(new CreateBackupPlanRequest
        {
            BackupPlan = new BackupPlanInput
            {
                BackupPlanName = model.BackupPlanName,
                Rules = [new BackupRuleInput { RuleName = model.RuleName, TargetBackupVaultName = model.TargetVaultName, ScheduleExpression = model.ScheduleExpression, Lifecycle = new Lifecycle { DeleteAfterDays = model.DeleteAfterDays } }]
            }
        });

    // ─── OpenSearch ────────────────────────────────────────────────────────────

    public async Task<List<OpenSearchDomainInfo>> ListOpenSearchDomainsAsync()
    {
        var resp = await openSearch.ListDomainNamesAsync(new ListDomainNamesRequest());
        var result = new List<OpenSearchDomainInfo>();
        foreach (var d in resp.DomainNames)
        {
            try
            {
                var desc = await openSearch.DescribeDomainAsync(new DescribeDomainRequest { DomainName = d.DomainName });
                var s = desc.DomainStatus;
                result.Add(new OpenSearchDomainInfo { DomainId = s.DomainId, DomainName = s.DomainName, ARN = s.ARN, Endpoint = s.Endpoint ?? "", EngineVersion = s.EngineVersion, Processing = s.Processing });
            }
            catch { result.Add(new OpenSearchDomainInfo { DomainName = d.DomainName }); }
        }
        return result;
    }

    public async Task CreateOpenSearchDomainAsync(CreateOpenSearchDomainViewModel model)
        => await openSearch.CreateDomainAsync(new CreateDomainRequest { DomainName = model.DomainName, EngineVersion = model.EngineVersion, ClusterConfig = new ClusterConfig { InstanceType = new OpenSearchPartitionInstanceType(model.InstanceType), InstanceCount = model.InstanceCount } });

    public async Task DeleteOpenSearchDomainAsync(string name) => await openSearch.DeleteDomainAsync(new DeleteDomainRequest { DomainName = name });

    // ─── Route 53 ──────────────────────────────────────────────────────────────

    public async Task<List<HostedZoneInfo>> ListHostedZonesAsync()
    {
        var resp = await route53.ListHostedZonesAsync(new ListHostedZonesRequest());
        return resp.HostedZones.Select(z => new HostedZoneInfo { Id = z.Id, Name = z.Name, PrivateZone = z.Config.PrivateZone, ResourceRecordSetCount = (int)z.ResourceRecordSetCount, Comment = z.Config.Comment ?? "" }).ToList();
    }

    public async Task CreateHostedZoneAsync(CreateHostedZoneViewModel model)
        => await route53.CreateHostedZoneAsync(new CreateHostedZoneRequest { Name = model.Name, CallerReference = Guid.NewGuid().ToString(), HostedZoneConfig = new HostedZoneConfig { Comment = model.Comment, PrivateZone = model.PrivateZone } });

    public async Task DeleteHostedZoneAsync(string id) => await route53.DeleteHostedZoneAsync(new DeleteHostedZoneRequest { Id = id });

    // ─── AppConfig ─────────────────────────────────────────────────────────────

    public async Task<List<AppConfigApplicationInfo>> ListAppConfigApplicationsAsync()
    {
        var resp = await appConfig.ListApplicationsAsync(new AppConfigListApplicationsRequest());
        return resp.Items.Select(a => new AppConfigApplicationInfo { Id = a.Id, Name = a.Name, Description = a.Description }).ToList();
    }

    public async Task<List<AppConfigEnvironmentInfo>> ListAppConfigEnvironmentsAsync(string applicationId)
    {
        var resp = await appConfig.ListEnvironmentsAsync(new ListEnvironmentsRequest { ApplicationId = applicationId });
        return resp.Items.Select(e => new AppConfigEnvironmentInfo { ApplicationId = applicationId, Id = e.Id, Name = e.Name, State = e.State?.Value ?? "" }).ToList();
    }

    public async Task CreateAppConfigApplicationAsync(CreateAppConfigApplicationViewModel model)
        => await appConfig.CreateApplicationAsync(new AppConfigCreateApplicationRequest { Name = model.Name, Description = model.Description });

    public async Task DeleteAppConfigApplicationAsync(string id) => await appConfig.DeleteApplicationAsync(new AppConfigDeleteApplicationRequest { ApplicationId = id });

    public async Task CreateAppConfigEnvironmentAsync(CreateAppConfigEnvironmentViewModel model)
        => await appConfig.CreateEnvironmentAsync(new CreateEnvironmentRequest { ApplicationId = model.ApplicationId, Name = model.Name, Description = model.Description });

    // ─── Transfer Family ───────────────────────────────────────────────────────

    public async Task<List<TransferServerInfo>> ListTransferServersAsync()
    {
        var resp = await transfer.ListServersAsync(new ListServersRequest());
        return resp.Servers.Select(s => new TransferServerInfo { ServerId = s.ServerId, Arn = s.Arn, State = s.State.Value, Domain = s.Domain.Value, EndpointType = s.EndpointType.Value, Protocols = [], IdentityProviderType = s.IdentityProviderType.Value }).ToList();
    }

    public async Task CreateTransferServerAsync(CreateTransferServerViewModel model)
        => await transfer.CreateServerAsync(new CreateServerRequest { Domain = new Domain(model.Domain), EndpointType = new EndpointType(model.EndpointType), Protocols = model.Protocols.Split(',').Select(p => p.Trim()).ToList(), IdentityProviderType = new TransferIdentityProviderType(model.IdentityProviderType) });

    public async Task DeleteTransferServerAsync(string serverId) => await transfer.DeleteServerAsync(new DeleteServerRequest { ServerId = serverId });
}
