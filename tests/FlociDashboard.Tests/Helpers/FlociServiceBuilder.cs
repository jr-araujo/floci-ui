using Amazon.AppConfig;
using Amazon.Athena;
using Amazon.AutoScaling;
using Amazon.Backup;
using Amazon.CloudFormation;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ElastiCache;
using Amazon.ElasticLoadBalancingV2;
using Amazon.EventBridge;
using Amazon.Glue;
using Amazon.IdentityManagement;
using Amazon.Kinesis;
using Amazon.KinesisFirehose;
using Amazon.KeyManagementService;
using Amazon.Lambda;
using Amazon.OpenSearchService;
using Amazon.RDS;
using Amazon.Route53;
using Amazon.S3;
using Amazon.Scheduler;
using Amazon.SecretsManager;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using Amazon.StepFunctions;
using Amazon.Transfer;
using FlociDashboard.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlociDashboard.Tests.Helpers;

public class FlociServiceBuilder
{
    public Mock<IAmazonS3> S3 { get; } = new();
    public Mock<IAmazonDynamoDB> DynamoDB { get; } = new();
    public Mock<IAmazonSQS> SQS { get; } = new();
    public Mock<IAmazonSimpleNotificationService> SNS { get; } = new();
    public Mock<IAmazonLambda> Lambda { get; } = new();
    public Mock<IAmazonKeyManagementService> KMS { get; } = new();
    public Mock<IAmazonIdentityManagementService> IAM { get; } = new();
    public Mock<IAmazonSimpleSystemsManagement> SSM { get; } = new();
    public Mock<IAmazonSecretsManager> Secrets { get; } = new();
    public Mock<IAmazonSimpleEmailService> SES { get; } = new();
    public Mock<IAmazonCognitoIdentityProvider> Cognito { get; } = new();
    public Mock<IAmazonKinesis> Kinesis { get; } = new();
    public Mock<IAmazonKinesisFirehose> Firehose { get; } = new();
    public Mock<IAmazonStepFunctions> SFN { get; } = new();
    public Mock<IAmazonCloudFormation> CFN { get; } = new();
    public Mock<IAmazonEventBridge> EventBridge { get; } = new();
    public Mock<IAmazonScheduler> Scheduler { get; } = new();
    public Mock<IAmazonCloudWatch> CloudWatch { get; } = new();
    public Mock<IAmazonCloudWatchLogs> CloudWatchLogs { get; } = new();
    public Mock<IAmazonElastiCache> ElastiCache { get; } = new();
    public Mock<IAmazonRDS> RDS { get; } = new();
    public Mock<IAmazonGlue> Glue { get; } = new();
    public Mock<IAmazonAthena> Athena { get; } = new();
    public Mock<IAmazonECS> ECS { get; } = new();
    public Mock<IAmazonECR> ECR { get; } = new();
    public Mock<IAmazonElasticLoadBalancingV2> ELB { get; } = new();
    public Mock<IAmazonAutoScaling> AutoScaling { get; } = new();
    public Mock<IAmazonBackup> Backup { get; } = new();
    public Mock<IAmazonOpenSearchService> OpenSearch { get; } = new();
    public Mock<IAmazonRoute53> Route53 { get; } = new();
    public Mock<IAmazonAppConfig> AppConfig { get; } = new();
    public Mock<IAmazonTransfer> Transfer { get; } = new();
    public Mock<IConfiguration> Config { get; } = new();
    public Mock<ILogger<FlociService>> Logger { get; } = new();

    public FlociServiceBuilder WithConfigValue(string key, string value)
    {
        Config.Setup(c => c[key]).Returns(value);
        return this;
    }

    public FlociService Build() => new(
        S3.Object, DynamoDB.Object, SQS.Object, SNS.Object, Lambda.Object,
        KMS.Object, IAM.Object, SSM.Object, Secrets.Object, SES.Object,
        Cognito.Object, Kinesis.Object, Firehose.Object, SFN.Object, CFN.Object,
        EventBridge.Object, Scheduler.Object, CloudWatch.Object, CloudWatchLogs.Object,
        ElastiCache.Object, RDS.Object, Glue.Object, Athena.Object, ECS.Object,
        ECR.Object, ELB.Object, AutoScaling.Object, Backup.Object, OpenSearch.Object,
        Route53.Object, AppConfig.Object, Transfer.Object, Config.Object, Logger.Object);
}
