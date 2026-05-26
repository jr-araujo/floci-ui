using FlociDashboard.Models;
using FlociDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlociDashboard.Controllers;

public class ServicesController(FlociService floci, RegionService regionService, ILogger<ServicesController> logger) : Controller
{
    private void SetRegionViewBag()
    {
        ViewBag.CurrentRegion = regionService.CurrentRegion;
        ViewBag.AvailableRegions = regionService.AvailableRegions;
    }

    // ─── S3 ──────────────────────────────────────────────────────────────────

    public async Task<IActionResult> S3()
    {
        SetRegionViewBag();
        var buckets = await SafeAsync(() => floci.ListBucketsAsync(), new List<S3BucketInfo>());
        return View(buckets);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBucket(CreateBucketViewModel model)
    {
        try { await floci.CreateBucketAsync(model.BucketName); TempData["Success"] = $"Bucket '{model.BucketName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(S3));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBucket(string name)
    {
        try { await floci.DeleteBucketAsync(name); TempData["Success"] = $"Bucket '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(S3));
    }

    public async Task<IActionResult> S3Objects(string bucket, string? prefix)
    {
        SetRegionViewBag();
        var objects = await SafeAsync(() => floci.ListObjectsAsync(bucket, prefix), new List<S3ObjectInfo>());
        ViewBag.Bucket = bucket;
        ViewBag.Prefix = prefix;
        return View(objects);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteObject(string bucket, string key)
    {
        try { await floci.DeleteObjectAsync(bucket, key); TempData["Success"] = $"Object '{key}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(S3Objects), new { bucket });
    }

    // ─── DynamoDB ────────────────────────────────────────────────────────────

    public async Task<IActionResult> DynamoDB()
    {
        SetRegionViewBag();
        var tables = await SafeAsync(() => floci.ListTablesAsync(), new List<DynamoTableInfo>());
        return View(tables);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTable(CreateTableViewModel model)
    {
        try { await floci.CreateTableAsync(model.TableName, model.PartitionKey, model.SortKey, model.EnableStreams, model.StreamViewType); TempData["Success"] = $"Table '{model.TableName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(DynamoDB));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTable(string name)
    {
        try { await floci.DeleteTableAsync(name); TempData["Success"] = $"Table '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(DynamoDB));
    }

    // ─── SQS ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> SQS()
    {
        SetRegionViewBag();
        var queues = await SafeAsync(() => floci.ListQueuesAsync(), new List<SqsQueueInfo>());
        return View(queues);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQueue(CreateQueueViewModel model)
    {
        try { await floci.CreateQueueAsync(model); TempData["Success"] = $"Queue '{model.QueueName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SQS));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQueue(string url)
    {
        var name = url.Split('/').Last();
        try { await floci.DeleteQueueAsync(url); TempData["Success"] = $"Queue '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SQS));
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(SendMessageViewModel model)
    {
        try { await floci.SendMessageAsync(model); TempData["Success"] = "Message sent successfully."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SQS));
    }

    public async Task<IActionResult> SQSMessages(string queueUrl, string queueName, int maxMessages = 10, int waitTimeSeconds = 0)
    {
        SetRegionViewBag();
        var model = new ReceiveMessagesViewModel { QueueUrl = queueUrl, QueueName = queueName, MaxMessages = maxMessages, WaitTimeSeconds = waitTimeSeconds };
        var messages = await SafeAsync(() => floci.ReceiveMessagesAsync(model), new List<SqsMessageInfo>());
        ViewBag.QueueUrl = queueUrl;
        ViewBag.QueueName = queueName;
        ViewBag.MaxMessages = maxMessages;
        ViewBag.WaitTimeSeconds = waitTimeSeconds;
        return View(messages);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMessage(string queueUrl, string queueName, string receiptHandle)
    {
        try { await floci.DeleteMessageAsync(queueUrl, receiptHandle); TempData["Success"] = "Message deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SQSMessages), new { queueUrl, queueName });
    }

    // ─── SNS ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> SNS()
    {
        SetRegionViewBag();
        var topics = await SafeAsync(() => floci.ListTopicsAsync(), new List<SnsTopicInfo>());
        var queues = await SafeAsync(() => floci.ListQueuesAsync(), new List<SqsQueueInfo>());
        ViewBag.Queues = queues;
        return View(topics);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTopic(CreateTopicViewModel model)
    {
        try { await floci.CreateTopicAsync(model); TempData["Success"] = $"Topic '{model.TopicName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SNS));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTopic(string arn)
    {
        var name = arn.Split(':').Last();
        try { await floci.DeleteTopicAsync(arn); TempData["Success"] = $"Topic '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SNS));
    }

    [HttpPost]
    public async Task<IActionResult> Publish(PublishViewModel model)
    {
        try { await floci.PublishAsync(model); TempData["Success"] = "Message published successfully."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SNS));
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(SnsSubscribeViewModel model)
    {
        try { await floci.SubscribeAsync(model); TempData["Success"] = $"Subscribed '{model.Endpoint}' to topic successfully."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SNS));
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe(string subscriptionArn)
    {
        try { await floci.UnsubscribeAsync(subscriptionArn); TempData["Success"] = "Subscription removed."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SNS));
    }

    // ─── Lambda ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Lambda()
    {
        SetRegionViewBag();
        var fns = await SafeAsync(() => floci.ListFunctionsAsync(), new List<LambdaFunctionInfo>());
        return View(fns);
    }

    [HttpPost]
    public async Task<IActionResult> InvokeFunction(InvokeFunctionViewModel model)
    {
        try
        {
            model.Result = await floci.InvokeFunctionAsync(model.FunctionName, model.Payload);
            TempData["InvokeResult"] = model.Result;
            TempData["InvokeName"] = model.FunctionName;
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Lambda));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFunction(string name)
    {
        try { await floci.DeleteFunctionAsync(name); TempData["Success"] = $"Function '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Lambda));
    }

    // ─── KMS ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> KMS()
    {
        SetRegionViewBag();
        var keys = await SafeAsync(() => floci.ListKeysAsync(), new List<KmsKeyInfo>());
        return View(keys);
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey(string description)
    {
        try { await floci.CreateKeyAsync(description); TempData["Success"] = "KMS key created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(KMS));
    }

    // ─── IAM ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> IAM()
    {
        SetRegionViewBag();
        var users = await SafeAsync(() => floci.ListUsersAsync(), new List<IamUserInfo>());
        var roles = await SafeAsync(() => floci.ListRolesAsync(), new List<IamRoleInfo>());
        ViewBag.Roles = roles;
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateIamUserViewModel model)
    {
        try { await floci.CreateUserAsync(model.UserName, model.Path); TempData["Success"] = $"User '{model.UserName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(IAM));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string userName)
    {
        try { await floci.DeleteUserAsync(userName); TempData["Success"] = $"User '{userName}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(IAM));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateIamRoleViewModel model)
    {
        try { await floci.CreateRoleAsync(model.RoleName, model.AssumeRolePolicyDocument); TempData["Success"] = $"Role '{model.RoleName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(IAM));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        try { await floci.DeleteRoleAsync(roleName); TempData["Success"] = $"Role '{roleName}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(IAM));
    }

    // ─── SSM ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> SSM()
    {
        SetRegionViewBag();
        var parameters = await SafeAsync(() => floci.ListParametersAsync(), new List<SsmParameterInfo>());
        return View(parameters);
    }

    [HttpPost]
    public async Task<IActionResult> PutParameter(PutParameterViewModel model)
    {
        try { await floci.PutParameterAsync(model); TempData["Success"] = $"Parameter '{model.Name}' saved."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SSM));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteParameter(string name)
    {
        try { await floci.DeleteParameterAsync(name); TempData["Success"] = $"Parameter '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SSM));
    }

    // ─── Secrets Manager ─────────────────────────────────────────────────────

    public async Task<IActionResult> SecretsManager()
    {
        SetRegionViewBag();
        var secrets = await SafeAsync(() => floci.ListSecretsAsync(), new List<SecretInfo>());
        return View(secrets);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSecret(CreateSecretViewModel model)
    {
        try { await floci.CreateSecretAsync(model); TempData["Success"] = $"Secret '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SecretsManager));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSecret(string name)
    {
        try { await floci.DeleteSecretAsync(name); TempData["Success"] = $"Secret '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SecretsManager));
    }

    public async Task<IActionResult> GetSecretValue(string name)
    {
        try
        {
            var val = await floci.GetSecretValueAsync(name);
            TempData["SecretValue"] = val;
            TempData["SecretName"] = name;
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SecretsManager));
    }

    // ─── SES ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> SES()
    {
        SetRegionViewBag();
        var identities = await SafeAsync(() => floci.ListIdentitiesAsync(), new List<SesIdentityInfo>());
        return View(identities);
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmail(SesVerifyEmailViewModel model)
    {
        try { await floci.VerifyEmailAsync(model.EmailAddress); TempData["Success"] = $"Verification initiated for '{model.EmailAddress}'."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SES));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteIdentity(string identity)
    {
        try { await floci.DeleteIdentityAsync(identity); TempData["Success"] = $"Identity '{identity}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SES));
    }

    [HttpPost]
    public async Task<IActionResult> SendEmail(SesSendEmailViewModel model)
    {
        try { await floci.SendEmailAsync(model); TempData["Success"] = $"Email sent to '{model.To}'."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SES));
    }

    // ─── Cognito ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Cognito()
    {
        SetRegionViewBag();
        var pools = await SafeAsync(() => floci.ListUserPoolsAsync(), new List<CognitoUserPoolInfo>());
        return View(pools);
    }

    public async Task<IActionResult> CognitoUsers(string userPoolId, string? poolName)
    {
        SetRegionViewBag();
        var users = await SafeAsync(() => floci.ListUsersInPoolAsync(userPoolId), new List<CognitoUserInfo>());
        ViewBag.UserPoolId = userPoolId;
        ViewBag.PoolName = poolName ?? userPoolId;
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserPool(CreateUserPoolViewModel model)
    {
        try { await floci.CreateUserPoolAsync(model); TempData["Success"] = $"User pool '{model.PoolName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Cognito));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUserPool(string userPoolId)
    {
        try { await floci.DeleteUserPoolAsync(userPoolId); TempData["Success"] = "User pool deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Cognito));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCognitoUser(CreateCognitoUserViewModel model)
    {
        try { await floci.CreateCognitoUserAsync(model); TempData["Success"] = $"User '{model.Username}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(CognitoUsers), new { userPoolId = model.UserPoolId });
    }

    // ─── Kinesis ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Kinesis()
    {
        SetRegionViewBag();
        var streams = await SafeAsync(() => floci.ListStreamsAsync(), new List<KinesisStreamInfo>());
        return View(streams);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStream(CreateKinesisStreamViewModel model)
    {
        try { await floci.CreateStreamAsync(model); TempData["Success"] = $"Stream '{model.StreamName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Kinesis));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStream(string name)
    {
        try { await floci.DeleteStreamAsync(name); TempData["Success"] = $"Stream '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Kinesis));
    }

    [HttpPost]
    public async Task<IActionResult> PutKinesisRecord(KinesisPutRecordViewModel model)
    {
        try { await floci.PutKinesisRecordAsync(model); TempData["Success"] = "Record published to stream."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Kinesis));
    }

    // ─── Firehose ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Firehose()
    {
        SetRegionViewBag();
        var streams = await SafeAsync(() => floci.ListDeliveryStreamsAsync(), new List<FirehoseDeliveryStreamInfo>());
        return View(streams);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeliveryStream(CreateFirehoseStreamViewModel model)
    {
        try { await floci.CreateDeliveryStreamAsync(model); TempData["Success"] = $"Delivery stream '{model.DeliveryStreamName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Firehose));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDeliveryStream(string name)
    {
        try { await floci.DeleteDeliveryStreamAsync(name); TempData["Success"] = $"Delivery stream '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Firehose));
    }

    // ─── Step Functions ──────────────────────────────────────────────────────

    public async Task<IActionResult> StepFunctions()
    {
        SetRegionViewBag();
        var machines = await SafeAsync(() => floci.ListStateMachinesAsync(), new List<StateMachineInfo>());
        return View(machines);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStateMachine(CreateStateMachineViewModel model)
    {
        try { await floci.CreateStateMachineAsync(model); TempData["Success"] = $"State machine '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(StepFunctions));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStateMachine(string arn)
    {
        var name = arn.Split(':').Last();
        try { await floci.DeleteStateMachineAsync(arn); TempData["Success"] = $"State machine '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(StepFunctions));
    }

    [HttpPost]
    public async Task<IActionResult> StartExecution(StartExecutionViewModel model)
    {
        try
        {
            var execArn = await floci.StartExecutionAsync(model);
            TempData["Success"] = $"Execution started: {execArn}";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(StepFunctions));
    }

    // ─── CloudFormation ──────────────────────────────────────────────────────

    public async Task<IActionResult> CloudFormation()
    {
        SetRegionViewBag();
        var stacks = await SafeAsync(() => floci.ListStacksAsync(), new List<CloudFormationStackInfo>());
        return View(stacks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStack(CreateStackViewModel model)
    {
        try { await floci.CreateStackAsync(model); TempData["Success"] = $"Stack '{model.StackName}' creation initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(CloudFormation));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStack(string name)
    {
        try { await floci.DeleteStackAsync(name); TempData["Success"] = $"Stack '{name}' deletion initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(CloudFormation));
    }

    // ─── EventBridge ─────────────────────────────────────────────────────────

    public async Task<IActionResult> EventBridge()
    {
        SetRegionViewBag();
        var buses = await SafeAsync(() => floci.ListEventBusesAsync(), new List<EventBridgeBusInfo>());
        var rules = await SafeAsync(() => floci.ListRulesAsync(), new List<EventBridgeRuleInfo>());
        ViewBag.Rules = rules;
        return View(buses);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventBus(CreateEventBusViewModel model)
    {
        try { await floci.CreateEventBusAsync(model.Name); TempData["Success"] = $"Event bus '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EventBridge));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEventBus(string name)
    {
        try { await floci.DeleteEventBusAsync(name); TempData["Success"] = $"Event bus '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EventBridge));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventRule(CreateEventRuleViewModel model)
    {
        try { await floci.CreateRuleAsync(model); TempData["Success"] = $"Rule '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EventBridge));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEventRule(string name, string eventBusName)
    {
        try { await floci.DeleteRuleAsync(name, eventBusName); TempData["Success"] = $"Rule '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EventBridge));
    }

    [HttpPost]
    public async Task<IActionResult> PutEvent(PutEventViewModel model)
    {
        try { await floci.PutEventAsync(model); TempData["Success"] = "Event published."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(EventBridge));
    }

    // ─── Scheduler ───────────────────────────────────────────────────────────

    public async Task<IActionResult> Scheduler()
    {
        SetRegionViewBag();
        var schedules = await SafeAsync(() => floci.ListSchedulesAsync(), new List<SchedulerScheduleInfo>());
        return View(schedules);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel model)
    {
        try { await floci.CreateScheduleAsync(model); TempData["Success"] = $"Schedule '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Scheduler));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSchedule(string name, string groupName)
    {
        try { await floci.DeleteScheduleAsync(name, groupName); TempData["Success"] = $"Schedule '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Scheduler));
    }

    // ─── CloudWatch ──────────────────────────────────────────────────────────

    public async Task<IActionResult> CloudWatch()
    {
        SetRegionViewBag();
        var alarms = await SafeAsync(() => floci.ListAlarmsAsync(), new List<CloudWatchAlarmInfo>());
        var logGroups = await SafeAsync(() => floci.ListLogGroupsAsync(), new List<CloudWatchLogGroupInfo>());
        ViewBag.LogGroups = logGroups;
        return View(alarms);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLogGroup(CreateLogGroupViewModel model)
    {
        try { await floci.CreateLogGroupAsync(model); TempData["Success"] = $"Log group '{model.LogGroupName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(CloudWatch));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLogGroup(string name)
    {
        try { await floci.DeleteLogGroupAsync(name); TempData["Success"] = $"Log group '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(CloudWatch));
    }

    // ─── ElastiCache ─────────────────────────────────────────────────────────

    public async Task<IActionResult> ElastiCache()
    {
        SetRegionViewBag();
        var clusters = await SafeAsync(() => floci.ListCacheClustersAsync(), new List<ElastiCacheClusterInfo>());
        return View(clusters);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCacheCluster(CreateElastiCacheClusterViewModel model)
    {
        try { await floci.CreateCacheClusterAsync(model); TempData["Success"] = $"Cache cluster '{model.ClusterId}' creation initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ElastiCache));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCacheCluster(string clusterId)
    {
        try { await floci.DeleteCacheClusterAsync(clusterId); TempData["Success"] = $"Cache cluster '{clusterId}' deletion initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ElastiCache));
    }

    // ─── RDS ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> RDS()
    {
        SetRegionViewBag();
        var instances = await SafeAsync(() => floci.ListDbInstancesAsync(), new List<RdsInstanceInfo>());
        return View(instances);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDbInstance(CreateRdsInstanceViewModel model)
    {
        try { await floci.CreateDbInstanceAsync(model); TempData["Success"] = $"RDS instance '{model.DBInstanceIdentifier}' creation initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(RDS));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDbInstance(string id)
    {
        try { await floci.DeleteDbInstanceAsync(id); TempData["Success"] = $"RDS instance '{id}' deletion initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(RDS));
    }

    // ─── Glue ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Glue()
    {
        SetRegionViewBag();
        var databases = await SafeAsync(() => floci.ListGlueDatabasesAsync(), new List<GlueDatabaseInfo>());
        return View(databases);
    }

    public async Task<IActionResult> GlueTables(string databaseName)
    {
        SetRegionViewBag();
        var tables = await SafeAsync(() => floci.ListGlueTablesAsync(databaseName), new List<GlueTableInfo>());
        ViewBag.DatabaseName = databaseName;
        return View(tables);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGlueDatabase(CreateGlueDatabaseViewModel model)
    {
        try { await floci.CreateGlueDatabaseAsync(model); TempData["Success"] = $"Glue database '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Glue));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGlueDatabase(string name)
    {
        try { await floci.DeleteGlueDatabaseAsync(name); TempData["Success"] = $"Glue database '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Glue));
    }

    // ─── Athena ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Athena()
    {
        SetRegionViewBag();
        var workGroups = await SafeAsync(() => floci.ListWorkGroupsAsync(), new List<AthenaWorkGroupInfo>());
        return View(workGroups);
    }

    [HttpPost]
    public async Task<IActionResult> RunAthenaQuery(AthenaQueryViewModel model)
    {
        try
        {
            var execId = await floci.StartQueryExecutionAsync(model);
            TempData["Success"] = $"Query started. Execution ID: {execId}";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Athena));
    }

    // ─── ECS ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> ECS()
    {
        SetRegionViewBag();
        var clusters = await SafeAsync(() => floci.ListEcsClustersAsync(), new List<EcsClusterInfo>());
        var taskDefs = await SafeAsync(() => floci.ListTaskDefinitionsAsync(), new List<EcsTaskDefinitionInfo>());
        ViewBag.TaskDefinitions = taskDefs;
        return View(clusters);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEcsCluster(CreateEcsClusterViewModel model)
    {
        try { await floci.CreateEcsClusterAsync(model.ClusterName); TempData["Success"] = $"ECS cluster '{model.ClusterName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECS));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEcsCluster(string arn)
    {
        var name = arn.Split('/').Last();
        try { await floci.DeleteEcsClusterAsync(arn); TempData["Success"] = $"ECS cluster '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECS));
    }

    [HttpPost]
    public async Task<IActionResult> RegisterTaskDefinition(RegisterTaskDefinitionViewModel model)
    {
        try { await floci.RegisterTaskDefinitionAsync(model); TempData["Success"] = $"Task definition '{model.Family}' registered."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECS));
    }

    [HttpPost]
    public async Task<IActionResult> RunEcsTask(RunEcsTaskViewModel model)
    {
        try { await floci.RunEcsTaskAsync(model); TempData["Success"] = $"Task launched from '{model.TaskDefinition}'."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECS));
    }

    // ─── ECR ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> ECR()
    {
        SetRegionViewBag();
        var repos = await SafeAsync(() => floci.ListRepositoriesAsync(), new List<EcrRepositoryInfo>());
        return View(repos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEcrRepository(CreateEcrRepositoryViewModel model)
    {
        try { await floci.CreateRepositoryAsync(model); TempData["Success"] = $"Repository '{model.RepositoryName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECR));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEcrRepository(string name)
    {
        try { await floci.DeleteRepositoryAsync(name); TempData["Success"] = $"Repository '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ECR));
    }

    // ─── ELB ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> ELB()
    {
        SetRegionViewBag();
        var lbs = await SafeAsync(() => floci.ListLoadBalancersAsync(), new List<LoadBalancerInfo>());
        var tgs = await SafeAsync(() => floci.ListTargetGroupsAsync(), new List<TargetGroupInfo>());
        ViewBag.TargetGroups = tgs;
        return View(lbs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTargetGroup(CreateTargetGroupViewModel model)
    {
        try { await floci.CreateTargetGroupAsync(model); TempData["Success"] = $"Target group '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ELB));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLoadBalancer(string arn)
    {
        try { await floci.DeleteLoadBalancerAsync(arn); TempData["Success"] = "Load balancer deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ELB));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTargetGroup(string arn)
    {
        try { await floci.DeleteTargetGroupAsync(arn); TempData["Success"] = "Target group deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ELB));
    }

    // ─── Auto Scaling ─────────────────────────────────────────────────────────

    public async Task<IActionResult> AutoScaling()
    {
        SetRegionViewBag();
        var groups = await SafeAsync(() => floci.ListAutoScalingGroupsAsync(), new List<AutoScalingGroupInfo>());
        return View(groups);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAutoScalingGroup(string name)
    {
        try { await floci.DeleteAutoScalingGroupAsync(name); TempData["Success"] = $"Auto Scaling group '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AutoScaling));
    }

    // ─── Backup ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Backup()
    {
        SetRegionViewBag();
        var vaults = await SafeAsync(() => floci.ListBackupVaultsAsync(), new List<BackupVaultInfo>());
        var plans = await SafeAsync(() => floci.ListBackupPlansAsync(), new List<BackupPlanInfo>());
        ViewBag.Plans = plans;
        return View(vaults);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBackupVault(CreateBackupVaultViewModel model)
    {
        try { await floci.CreateBackupVaultAsync(model); TempData["Success"] = $"Backup vault '{model.BackupVaultName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Backup));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBackupVault(string name)
    {
        try { await floci.DeleteBackupVaultAsync(name); TempData["Success"] = $"Backup vault '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Backup));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBackupPlan(CreateBackupPlanViewModel model)
    {
        try { await floci.CreateBackupPlanAsync(model); TempData["Success"] = $"Backup plan '{model.BackupPlanName}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Backup));
    }

    // ─── OpenSearch ───────────────────────────────────────────────────────────

    public async Task<IActionResult> OpenSearch()
    {
        SetRegionViewBag();
        var domains = await SafeAsync(() => floci.ListOpenSearchDomainsAsync(), new List<OpenSearchDomainInfo>());
        return View(domains);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOpenSearchDomain(CreateOpenSearchDomainViewModel model)
    {
        try { await floci.CreateOpenSearchDomainAsync(model); TempData["Success"] = $"OpenSearch domain '{model.DomainName}' creation initiated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(OpenSearch));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOpenSearchDomain(string name)
    {
        try { await floci.DeleteOpenSearchDomainAsync(name); TempData["Success"] = $"OpenSearch domain '{name}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(OpenSearch));
    }

    // ─── Route53 ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Route53()
    {
        SetRegionViewBag();
        var zones = await SafeAsync(() => floci.ListHostedZonesAsync(), new List<HostedZoneInfo>());
        return View(zones);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHostedZone(CreateHostedZoneViewModel model)
    {
        try { await floci.CreateHostedZoneAsync(model); TempData["Success"] = $"Hosted zone '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Route53));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteHostedZone(string id)
    {
        try { await floci.DeleteHostedZoneAsync(id); TempData["Success"] = "Hosted zone deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Route53));
    }

    // ─── AppConfig ───────────────────────────────────────────────────────────

    public async Task<IActionResult> AppConfig()
    {
        SetRegionViewBag();
        var apps = await SafeAsync(() => floci.ListAppConfigApplicationsAsync(), new List<AppConfigApplicationInfo>());
        return View(apps);
    }

    public async Task<IActionResult> AppConfigEnvironments(string applicationId, string? appName)
    {
        SetRegionViewBag();
        var envs = await SafeAsync(() => floci.ListAppConfigEnvironmentsAsync(applicationId), new List<AppConfigEnvironmentInfo>());
        ViewBag.ApplicationId = applicationId;
        ViewBag.AppName = appName ?? applicationId;
        return View(envs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppConfigApplication(CreateAppConfigApplicationViewModel model)
    {
        try { await floci.CreateAppConfigApplicationAsync(model); TempData["Success"] = $"AppConfig application '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AppConfig));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAppConfigApplication(string id)
    {
        try { await floci.DeleteAppConfigApplicationAsync(id); TempData["Success"] = "AppConfig application deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AppConfig));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppConfigEnvironment(CreateAppConfigEnvironmentViewModel model)
    {
        try { await floci.CreateAppConfigEnvironmentAsync(model); TempData["Success"] = $"Environment '{model.Name}' created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AppConfigEnvironments), new { applicationId = model.ApplicationId });
    }

    // ─── Transfer Family ─────────────────────────────────────────────────────

    public async Task<IActionResult> Transfer()
    {
        SetRegionViewBag();
        var servers = await SafeAsync(() => floci.ListTransferServersAsync(), new List<TransferServerInfo>());
        return View(servers);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferServer(CreateTransferServerViewModel model)
    {
        try { await floci.CreateTransferServerAsync(model); TempData["Success"] = "Transfer server created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Transfer));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTransferServer(string serverId)
    {
        try { await floci.DeleteTransferServerAsync(serverId); TempData["Success"] = $"Transfer server '{serverId}' deleted."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Transfer));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<T> SafeAsync<T>(Func<Task<T>> fn, T fallback)
    {
        try { return await fn(); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Service call failed");
            ViewBag.ServiceError = ex.Message;
            return fallback;
        }
    }
}
