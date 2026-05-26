using Amazon;
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
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElastiCache;
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
using Amazon.Runtime;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddSession(opts =>
{
    opts.Cookie.HttpOnly = true;
    opts.Cookie.IsEssential = true;
    opts.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddHttpContextAccessor();

// Floci configuration
var flociConfig = builder.Configuration.GetSection("Floci");
var serviceUrl = flociConfig["ServiceUrl"] ?? "http://localhost:4566";
var credentials = new BasicAWSCredentials(
    flociConfig["AccessKey"] ?? "test",
    flociConfig["SecretKey"] ?? "test");

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true }));
builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, new AmazonSQSConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonLambda>(new AmazonLambdaClient(credentials, new AmazonLambdaConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonKeyManagementService>(new AmazonKeyManagementServiceClient(credentials, new AmazonKeyManagementServiceConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonIdentityManagementService>(new AmazonIdentityManagementServiceClient(credentials, new AmazonIdentityManagementServiceConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonSimpleSystemsManagement>(new AmazonSimpleSystemsManagementClient(credentials, new AmazonSimpleSystemsManagementConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonSecretsManager>(new AmazonSecretsManagerClient(credentials, new AmazonSecretsManagerConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonSimpleEmailService>(new AmazonSimpleEmailServiceClient(credentials, new AmazonSimpleEmailServiceConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(new AmazonCognitoIdentityProviderClient(credentials, new AmazonCognitoIdentityProviderConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonKinesis>(new AmazonKinesisClient(credentials, new AmazonKinesisConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonKinesisFirehose>(new AmazonKinesisFirehoseClient(credentials, new AmazonKinesisFirehoseConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonStepFunctions>(new AmazonStepFunctionsClient(credentials, new AmazonStepFunctionsConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonCloudFormation>(new AmazonCloudFormationClient(credentials, new AmazonCloudFormationConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonEventBridge>(new AmazonEventBridgeClient(credentials, new AmazonEventBridgeConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonScheduler>(new AmazonSchedulerClient(credentials, new AmazonSchedulerConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonCloudWatch>(new AmazonCloudWatchClient(credentials, new AmazonCloudWatchConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonCloudWatchLogs>(new AmazonCloudWatchLogsClient(credentials, new AmazonCloudWatchLogsConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonElastiCache>(new AmazonElastiCacheClient(credentials, new AmazonElastiCacheConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonRDS>(new AmazonRDSClient(credentials, new AmazonRDSConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonGlue>(new AmazonGlueClient(credentials, new AmazonGlueConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonAthena>(new AmazonAthenaClient(credentials, new AmazonAthenaConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonECS>(new AmazonECSClient(credentials, new AmazonECSConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonECR>(new AmazonECRClient(credentials, new AmazonECRConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonElasticLoadBalancingV2>(new AmazonElasticLoadBalancingV2Client(credentials, new AmazonElasticLoadBalancingV2Config { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonAutoScaling>(new AmazonAutoScalingClient(credentials, new AmazonAutoScalingConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonBackup>(new AmazonBackupClient(credentials, new AmazonBackupConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonOpenSearchService>(new AmazonOpenSearchServiceClient(credentials, new AmazonOpenSearchServiceConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonRoute53>(new AmazonRoute53Client(credentials, new AmazonRoute53Config { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonAppConfig>(new AmazonAppConfigClient(credentials, new AmazonAppConfigConfig { ServiceURL = serviceUrl }));
builder.Services.AddSingleton<IAmazonTransfer>(new AmazonTransferClient(credentials, new AmazonTransferConfig { ServiceURL = serviceUrl }));

builder.Services.AddSingleton(flociConfig);
builder.Services.AddScoped<FlociService>();
builder.Services.AddScoped<RegionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
