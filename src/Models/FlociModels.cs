namespace FlociDashboard.Models;

// ─── Dashboard ─────────────────────────────────────────────────────────────────

public class DashboardSummary
{
    // Storage & Data
    public int S3Buckets { get; set; }
    public int DynamoTables { get; set; }
    public int ElastiCacheClusters { get; set; }
    public int RdsInstances { get; set; }
    public int OpenSearchDomains { get; set; }
    public int BackupVaults { get; set; }
    // Messaging
    public int SqsQueues { get; set; }
    public int SnsTopics { get; set; }
    public int KinesisStreams { get; set; }
    public int FirehoseStreams { get; set; }
    public int SesIdentities { get; set; }
    public int EventBridgeBuses { get; set; }
    public int SchedulerSchedules { get; set; }
    // Compute
    public int LambdaFunctions { get; set; }
    public int EcsClusters { get; set; }
    public int EcrRepositories { get; set; }
    public int StepFunctionStateMachines { get; set; }
    public int ElbLoadBalancers { get; set; }
    public int AutoScalingGroups { get; set; }
    // Security & Identity
    public int KmsKeys { get; set; }
    public int IamUsers { get; set; }
    public int CognitoUserPools { get; set; }
    public int SecretsCount { get; set; }
    // Management
    public int SsmParameters { get; set; }
    public int CloudFormationStacks { get; set; }
    public int CloudWatchAlarms { get; set; }
    public int CloudWatchLogGroups { get; set; }
    public int AppConfigApplications { get; set; }
    // Analytics
    public int GlueDatabases { get; set; }
    public int AthenaWorkGroups { get; set; }
    // Network
    public int Route53Zones { get; set; }
    public int TransferServers { get; set; }
    public DateTime LastRefreshed { get; set; }
}

// ─── S3 ────────────────────────────────────────────────────────────────────────

public class S3BucketInfo
{
    public string Name { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public int ObjectCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public string FormattedSize => FormatBytes(TotalSizeBytes);

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public class S3ObjectInfo
{
    public string Key { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string ETag { get; set; } = "";
    public string StorageClass { get; set; } = "";
    public string FormattedSize => FormatBytes(Size);

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public class CreateBucketViewModel
{
    public string BucketName { get; set; } = "";
}

// ─── DynamoDB ──────────────────────────────────────────────────────────────────

public class DynamoTableInfo
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public long ItemCount { get; set; }
    public long SizeBytes { get; set; }
    public string BillingMode { get; set; } = "";
    public List<string> KeySchema { get; set; } = [];
    public bool StreamEnabled { get; set; }
    public string StreamViewType { get; set; } = "";
}

public class CreateTableViewModel
{
    public string TableName { get; set; } = "";
    public string PartitionKey { get; set; } = "id";
    public string SortKey { get; set; } = "";
    public bool EnableStreams { get; set; }
    public string StreamViewType { get; set; } = "NEW_AND_OLD_IMAGES";
}

// ─── SQS ───────────────────────────────────────────────────────────────────────

public class SqsQueueInfo
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Arn { get; set; } = "";
    public int MessagesAvailable { get; set; }
    public int MessagesInFlight { get; set; }
    public int VisibilityTimeout { get; set; }
    public int RetentionPeriod { get; set; }
    public bool IsFifo { get; set; }
    public string? DeadLetterTargetArn { get; set; }
    public int MaxReceiveCount { get; set; }
}

public class CreateQueueViewModel
{
    public string QueueName { get; set; } = "";
    public bool Fifo { get; set; }
    public int VisibilityTimeout { get; set; } = 30;
    public int MessageRetentionPeriod { get; set; } = 345600;
    public string? DeadLetterQueueArn { get; set; }
    public int MaxReceiveCount { get; set; } = 3;
}

public class SendMessageViewModel
{
    public string QueueUrl { get; set; } = "";
    public string QueueName { get; set; } = "";
    public string Body { get; set; } = "";
    public string? MessageGroupId { get; set; }
    public string? MessageDeduplicationId { get; set; }
}

public class ReceiveMessagesViewModel
{
    public string QueueUrl { get; set; } = "";
    public string QueueName { get; set; } = "";
    public int MaxMessages { get; set; } = 10;
    public int WaitTimeSeconds { get; set; } = 0;
}

public class SqsMessageInfo
{
    public string MessageId { get; set; } = "";
    public string ReceiptHandle { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime? SentTimestamp { get; set; }
    public string? SenderId { get; set; }
    public int ApproximateReceiveCount { get; set; }
    public Dictionary<string, string> MessageAttributes { get; set; } = [];
    public string QueueUrl { get; set; } = "";
    public string QueueName { get; set; } = "";
}

// ─── SNS ───────────────────────────────────────────────────────────────────────

public class SnsTopicInfo
{
    public string Arn { get; set; } = "";
    public string Name { get; set; } = "";
    public int SubscriptionsCount { get; set; }
    public bool IsFifo { get; set; }
    public List<SnsSubscriptionInfo> Subscriptions { get; set; } = [];
}

public class SnsSubscriptionInfo
{
    public string SubscriptionArn { get; set; } = "";
    public string Protocol { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string FilterPolicy { get; set; } = "";
}

public class CreateTopicViewModel
{
    public string TopicName { get; set; } = "";
    public bool Fifo { get; set; }
    public bool ContentBasedDeduplication { get; set; }
}

public class PublishViewModel
{
    public string TopicArn { get; set; } = "";
    public string TopicName { get; set; } = "";
    public string Message { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? MessageAttributes { get; set; }
}

public class SnsSubscribeViewModel
{
    public string TopicArn { get; set; } = "";
    public string TopicName { get; set; } = "";
    public string Protocol { get; set; } = "sqs";
    public string Endpoint { get; set; } = "";
    public string? FilterPolicy { get; set; }
}

// ─── Lambda ────────────────────────────────────────────────────────────────────

public class LambdaFunctionInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Runtime { get; set; } = "";
    public string Handler { get; set; } = "";
    public int MemorySize { get; set; }
    public int Timeout { get; set; }
    public string LastModified { get; set; } = "";
    public string State { get; set; } = "";
    public string Description { get; set; } = "";
}

public class InvokeFunctionViewModel
{
    public string FunctionName { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string? Result { get; set; }
}

// ─── KMS ───────────────────────────────────────────────────────────────────────

public class KmsKeyInfo
{
    public string KeyId { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Description { get; set; } = "";
    public string KeyState { get; set; } = "";
    public string KeyUsage { get; set; } = "";
    public DateTime CreationDate { get; set; }
}

// ─── IAM ───────────────────────────────────────────────────────────────────────

public class IamUserInfo
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Arn { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public string Path { get; set; } = "";
}

public class IamRoleInfo
{
    public string RoleId { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string Arn { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public string AssumeRolePolicyDocument { get; set; } = "";
}

public class CreateIamUserViewModel
{
    public string UserName { get; set; } = "";
    public string Path { get; set; } = "/";
}

public class CreateIamRoleViewModel
{
    public string RoleName { get; set; } = "";
    public string AssumeRolePolicyDocument { get; set; } = """{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}""";
}

// ─── SSM ───────────────────────────────────────────────────────────────────────

public class SsmParameterInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime LastModifiedDate { get; set; }
    public string Description { get; set; } = "";
    public long Version { get; set; }
}

public class PutParameterViewModel
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "String";
    public string Description { get; set; } = "";
    public bool Overwrite { get; set; } = true;
}

// ─── Secrets Manager ───────────────────────────────────────────────────────────

public class SecretInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public DateTime? LastChangedDate { get; set; }
    public string Description { get; set; } = "";
    public string? KmsKeyId { get; set; }
}

public class CreateSecretViewModel
{
    public string Name { get; set; } = "";
    public string SecretString { get; set; } = "";
    public string Description { get; set; } = "";
    public string? KmsKeyId { get; set; }
}

// ─── SES ───────────────────────────────────────────────────────────────────────

public class SesIdentityInfo
{
    public string Identity { get; set; } = "";
    public string VerificationStatus { get; set; } = "";
    public bool IsEmail { get; set; }
}

public class SesVerifyEmailViewModel
{
    public string EmailAddress { get; set; } = "";
}

public class SesSendEmailViewModel
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; }
}

// ─── Cognito ───────────────────────────────────────────────────────────────────

public class CognitoUserPoolInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public int UserCount { get; set; }
    public string Status { get; set; } = "";
}

public class CognitoUserInfo
{
    public string Username { get; set; } = "";
    public string UserStatus { get; set; } = "";
    public DateTime UserCreateDate { get; set; }
    public List<string> Attributes { get; set; } = [];
}

public class CreateUserPoolViewModel
{
    public string PoolName { get; set; } = "";
    public bool UsernameAsEmail { get; set; }
    public bool MfaEnabled { get; set; }
}

public class CreateCognitoUserViewModel
{
    public string UserPoolId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";
}

// ─── Kinesis ───────────────────────────────────────────────────────────────────

public class KinesisStreamInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Status { get; set; } = "";
    public int ShardCount { get; set; }
    public DateTime CreationTimestamp { get; set; }
    public string StreamMode { get; set; } = "PROVISIONED";
}

public class CreateKinesisStreamViewModel
{
    public string StreamName { get; set; } = "";
    public int ShardCount { get; set; } = 1;
    public string StreamMode { get; set; } = "PROVISIONED";
}

public class KinesisPutRecordViewModel
{
    public string StreamName { get; set; } = "";
    public string Data { get; set; } = "";
    public string PartitionKey { get; set; } = "default";
}

// ─── Kinesis Firehose ──────────────────────────────────────────────────────────

public class FirehoseDeliveryStreamInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Status { get; set; } = "";
    public string DeliveryStreamType { get; set; } = "";
    public DateTime CreateTimestamp { get; set; }
}

public class CreateFirehoseStreamViewModel
{
    public string DeliveryStreamName { get; set; } = "";
    public string DeliveryStreamType { get; set; } = "DirectPut";
    public string DestinationType { get; set; } = "S3";
    public string S3BucketArn { get; set; } = "";
    public string KinesisSourceStreamArn { get; set; } = "";
}

// ─── Step Functions ────────────────────────────────────────────────────────────

public class StateMachineInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Status { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime CreationDate { get; set; }
}

public class CreateStateMachineViewModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "STANDARD";
    public string Definition { get; set; } = """{"Comment":"My state machine","StartAt":"HelloWorld","States":{"HelloWorld":{"Type":"Pass","End":true}}}""";
    public string RoleArn { get; set; } = "";
}

public class StartExecutionViewModel
{
    public string StateMachineArn { get; set; } = "";
    public string StateMachineName { get; set; } = "";
    public string Input { get; set; } = "{}";
    public string ExecutionName { get; set; } = "";
}

public class StateMachineExecutionInfo
{
    public string ExecutionArn { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? StopDate { get; set; }
}

// ─── CloudFormation ────────────────────────────────────────────────────────────

public class CloudFormationStackInfo
{
    public string StackId { get; set; } = "";
    public string StackName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreationTime { get; set; }
    public string Description { get; set; } = "";
    public int ResourceCount { get; set; }
}

public class CreateStackViewModel
{
    public string StackName { get; set; } = "";
    public string TemplateBody { get; set; } = """{"AWSTemplateFormatVersion":"2010-09-09","Resources":{"MyBucket":{"Type":"AWS::S3::Bucket"}}}""";
    public string Parameters { get; set; } = "";
}

// ─── EventBridge ───────────────────────────────────────────────────────────────

public class EventBridgeBusInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
}

public class EventBridgeRuleInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string State { get; set; } = "";
    public string EventPattern { get; set; } = "";
    public string ScheduleExpression { get; set; } = "";
    public string EventBusName { get; set; } = "";
}

public class CreateEventBusViewModel
{
    public string Name { get; set; } = "";
}

public class CreateEventRuleViewModel
{
    public string Name { get; set; } = "";
    public string EventBusName { get; set; } = "default";
    public string EventPattern { get; set; } = """{"source":["my.app"]}""";
    public string ScheduleExpression { get; set; } = "";
    public string State { get; set; } = "ENABLED";
}

public class PutEventViewModel
{
    public string EventBusName { get; set; } = "default";
    public string Source { get; set; } = "";
    public string DetailType { get; set; } = "";
    public string Detail { get; set; } = "{}";
}

// ─── EventBridge Scheduler ─────────────────────────────────────────────────────

public class SchedulerScheduleInfo
{
    public string Name { get; set; } = "";
    public string Arn { get; set; } = "";
    public string State { get; set; } = "";
    public string ScheduleExpression { get; set; } = "";
    public string GroupName { get; set; } = "";
}

public class CreateScheduleViewModel
{
    public string Name { get; set; } = "";
    public string GroupName { get; set; } = "default";
    public string ScheduleExpression { get; set; } = "rate(5 minutes)";
    public string TargetArn { get; set; } = "";
    public string TargetRoleArn { get; set; } = "";
    public string Input { get; set; } = "{}";
    public string State { get; set; } = "ENABLED";
}

// ─── CloudWatch ────────────────────────────────────────────────────────────────

public class CloudWatchAlarmInfo
{
    public string AlarmName { get; set; } = "";
    public string AlarmArn { get; set; } = "";
    public string StateValue { get; set; } = "";
    public string MetricName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string ComparisonOperator { get; set; } = "";
    public double Threshold { get; set; }
}

public class CloudWatchLogGroupInfo
{
    public string LogGroupName { get; set; } = "";
    public long StoredBytes { get; set; }
    public int? RetentionInDays { get; set; }
    public DateTime? CreationTime { get; set; }
}

public class CreateLogGroupViewModel
{
    public string LogGroupName { get; set; } = "";
    public int? RetentionInDays { get; set; }
}

public class PutLogEventsViewModel
{
    public string LogGroupName { get; set; } = "";
    public string LogStreamName { get; set; } = "";
    public string Message { get; set; } = "";
}

// ─── ElastiCache ───────────────────────────────────────────────────────────────

public class ElastiCacheClusterInfo
{
    public string ClusterId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Engine { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public string CacheNodeType { get; set; } = "";
    public int NumCacheNodes { get; set; }
}

public class CreateElastiCacheClusterViewModel
{
    public string ClusterId { get; set; } = "";
    public string Engine { get; set; } = "redis";
    public string EngineVersion { get; set; } = "7.0";
    public string CacheNodeType { get; set; } = "cache.t3.micro";
    public int NumCacheNodes { get; set; } = 1;
}

// ─── RDS ───────────────────────────────────────────────────────────────────────

public class RdsInstanceInfo
{
    public string DBInstanceIdentifier { get; set; } = "";
    public string DBInstanceStatus { get; set; } = "";
    public string Engine { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public string DBInstanceClass { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public int Port { get; set; }
    public string MasterUsername { get; set; } = "";
}

public class CreateRdsInstanceViewModel
{
    public string DBInstanceIdentifier { get; set; } = "";
    public string Engine { get; set; } = "postgres";
    public string EngineVersion { get; set; } = "15";
    public string DBInstanceClass { get; set; } = "db.t3.micro";
    public string MasterUsername { get; set; } = "admin";
    public string MasterUserPassword { get; set; } = "";
    public int AllocatedStorage { get; set; } = 20;
    public string DBName { get; set; } = "";
}

// ─── Glue ──────────────────────────────────────────────────────────────────────

public class GlueDatabaseInfo
{
    public string Name { get; set; } = "";
    public DateTime? CreateTime { get; set; }
    public string Description { get; set; } = "";
    public int TableCount { get; set; }
}

public class GlueTableInfo
{
    public string Name { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public DateTime? CreateTime { get; set; }
    public string TableType { get; set; } = "";
}

public class CreateGlueDatabaseViewModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class CreateGlueCrawlerViewModel
{
    public string Name { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string S3TargetPath { get; set; } = "";
    public string Role { get; set; } = "";
}

// ─── Athena ────────────────────────────────────────────────────────────────────

public class AthenaWorkGroupInfo
{
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public string Description { get; set; } = "";
    public string OutputLocation { get; set; } = "";
}

public class AthenaQueryViewModel
{
    public string WorkGroup { get; set; } = "primary";
    public string QueryString { get; set; } = "SELECT 1";
    public string OutputLocation { get; set; } = "";
    public string? QueryExecutionId { get; set; }
    public string? QueryStatus { get; set; }
}

// ─── ECS ───────────────────────────────────────────────────────────────────────

public class EcsClusterInfo
{
    public string ClusterArn { get; set; } = "";
    public string ClusterName { get; set; } = "";
    public string Status { get; set; } = "";
    public int RunningTasksCount { get; set; }
    public int ActiveServicesCount { get; set; }
    public int RegisteredContainerInstancesCount { get; set; }
}

public class EcsTaskDefinitionInfo
{
    public string TaskDefinitionArn { get; set; } = "";
    public string Family { get; set; } = "";
    public string Status { get; set; } = "";
    public int Revision { get; set; }
    public string NetworkMode { get; set; } = "";
    public List<string> ContainerNames { get; set; } = [];
}

public class CreateEcsClusterViewModel
{
    public string ClusterName { get; set; } = "";
}

public class RegisterTaskDefinitionViewModel
{
    public string Family { get; set; } = "";
    public string NetworkMode { get; set; } = "awsvpc";
    public string ContainerName { get; set; } = "";
    public string Image { get; set; } = "";
    public int Cpu { get; set; } = 256;
    public int Memory { get; set; } = 512;
    public int ContainerPort { get; set; } = 80;
    public string ExecutionRoleArn { get; set; } = "";
}

public class RunEcsTaskViewModel
{
    public string ClusterArn { get; set; } = "";
    public string ClusterName { get; set; } = "";
    public string TaskDefinition { get; set; } = "";
    public int Count { get; set; } = 1;
    public string LaunchType { get; set; } = "FARGATE";
}

// ─── ECR ───────────────────────────────────────────────────────────────────────

public class EcrRepositoryInfo
{
    public string RepositoryArn { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string RepositoryUri { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string ImageTagMutability { get; set; } = "";
    public int ImageCount { get; set; }
}

public class CreateEcrRepositoryViewModel
{
    public string RepositoryName { get; set; } = "";
    public string ImageTagMutability { get; set; } = "MUTABLE";
    public bool ScanOnPush { get; set; }
}

// ─── ELB v2 ────────────────────────────────────────────────────────────────────

public class LoadBalancerInfo
{
    public string LoadBalancerArn { get; set; } = "";
    public string LoadBalancerName { get; set; } = "";
    public string Type { get; set; } = "";
    public string Scheme { get; set; } = "";
    public string State { get; set; } = "";
    public string DNSName { get; set; } = "";
    public DateTime CreatedTime { get; set; }
}

public class TargetGroupInfo
{
    public string TargetGroupArn { get; set; } = "";
    public string TargetGroupName { get; set; } = "";
    public string Protocol { get; set; } = "";
    public int Port { get; set; }
    public string TargetType { get; set; } = "";
    public string VpcId { get; set; } = "";
}

public class CreateLoadBalancerViewModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "application";
    public string Scheme { get; set; } = "internet-facing";
    public List<string> SubnetIds { get; set; } = [];
}

public class CreateTargetGroupViewModel
{
    public string Name { get; set; } = "";
    public string Protocol { get; set; } = "HTTP";
    public int Port { get; set; } = 80;
    public string TargetType { get; set; } = "ip";
    public string VpcId { get; set; } = "";
}

// ─── Auto Scaling ──────────────────────────────────────────────────────────────

public class AutoScalingGroupInfo
{
    public string AutoScalingGroupName { get; set; } = "";
    public string AutoScalingGroupArn { get; set; } = "";
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public int DesiredCapacity { get; set; }
    public string Status { get; set; } = "";
    public List<string> AvailabilityZones { get; set; } = [];
}

public class CreateAutoScalingGroupViewModel
{
    public string AutoScalingGroupName { get; set; } = "";
    public string LaunchConfigurationName { get; set; } = "";
    public int MinSize { get; set; } = 1;
    public int MaxSize { get; set; } = 3;
    public int DesiredCapacity { get; set; } = 1;
    public string AvailabilityZones { get; set; } = "us-east-1a";
}

// ─── Backup ────────────────────────────────────────────────────────────────────

public class BackupVaultInfo
{
    public string BackupVaultName { get; set; } = "";
    public string BackupVaultArn { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public int NumberOfRecoveryPoints { get; set; }
    public string EncryptionKeyArn { get; set; } = "";
}

public class BackupPlanInfo
{
    public string BackupPlanId { get; set; } = "";
    public string BackupPlanName { get; set; } = "";
    public string BackupPlanArn { get; set; } = "";
    public DateTime CreationDate { get; set; }
}

public class CreateBackupVaultViewModel
{
    public string BackupVaultName { get; set; } = "";
    public string? EncryptionKeyArn { get; set; }
}

public class CreateBackupPlanViewModel
{
    public string BackupPlanName { get; set; } = "";
    public string RuleName { get; set; } = "DailyBackup";
    public string TargetVaultName { get; set; } = "";
    public string ScheduleExpression { get; set; } = "cron(0 5 ? * * *)";
    public int DeleteAfterDays { get; set; } = 30;
}

// ─── OpenSearch ────────────────────────────────────────────────────────────────

public class OpenSearchDomainInfo
{
    public string DomainId { get; set; } = "";
    public string DomainName { get; set; } = "";
    public string ARN { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public bool Processing { get; set; }
}

public class CreateOpenSearchDomainViewModel
{
    public string DomainName { get; set; } = "";
    public string EngineVersion { get; set; } = "OpenSearch_2.11";
    public string InstanceType { get; set; } = "t3.small.search";
    public int InstanceCount { get; set; } = 1;
}

// ─── Route 53 ──────────────────────────────────────────────────────────────────

public class HostedZoneInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool PrivateZone { get; set; }
    public int ResourceRecordSetCount { get; set; }
    public string Comment { get; set; } = "";
}

public class CreateHostedZoneViewModel
{
    public string Name { get; set; } = "";
    public bool PrivateZone { get; set; }
    public string Comment { get; set; } = "";
}

// ─── AppConfig ─────────────────────────────────────────────────────────────────

public class AppConfigApplicationInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class AppConfigEnvironmentInfo
{
    public string ApplicationId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
}

public class AppConfigProfileInfo
{
    public string ApplicationId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string LocationUri { get; set; } = "";
    public string Type { get; set; } = "";
}

public class CreateAppConfigApplicationViewModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class CreateAppConfigEnvironmentViewModel
{
    public string ApplicationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

// ─── Transfer Family ───────────────────────────────────────────────────────────

public class TransferServerInfo
{
    public string ServerId { get; set; } = "";
    public string Arn { get; set; } = "";
    public string State { get; set; } = "";
    public string Domain { get; set; } = "";
    public string EndpointType { get; set; } = "";
    public List<string> Protocols { get; set; } = [];
    public string IdentityProviderType { get; set; } = "";
}

public class CreateTransferServerViewModel
{
    public string Domain { get; set; } = "S3";
    public string EndpointType { get; set; } = "PUBLIC";
    public string Protocols { get; set; } = "SFTP";
    public string IdentityProviderType { get; set; } = "SERVICE_MANAGED";
}
