# floci-ui

Floci UI — a lightweight Razor Pages dashboard for managing Floci integrations and AWS-compatible services (built on .NET 9).

## 1. What is this project?
floci-ui is the frontend/admin portal for the Floci system. It provides a unified UI to inspect and operate AWS-compatible services (LocalStack-ready), view configs, and run common operational tasks.

## 2. Available services
The UI exposes connectors for the following AWS-compatible services:
- Storage: S3
- Databases: DynamoDB, RDS, ElastiCache
- Messaging & Streaming: SQS, SNS, Kinesis, Kinesis Firehose
- Compute & Containers: Lambda, ECS, ECR
- Identity & Security: IAM, KMS, Cognito, Secrets Manager, SSM
- Observability & Ops: CloudWatch, CloudWatch Logs, EventBridge, Step Functions
- Analytics & ETL: Glue, Athena, OpenSearch Service
- Networking & DNS: Route53, Elastic Load Balancing (ALB/NLB)
- Others: AppConfig, Scheduler, Transfer, Backup, AutoScaling

## 3. How to use each service on the UI
Open the left-hand "Services" menu (or top nav) and select a service:

- S3: view buckets, list objects, upload/download, create/delete buckets.
- DynamoDB: browse tables, query items, create/delete tables.
- SQS / SNS: list queues/topics, send test messages, view attributes.
- Lambda: list functions, view configuration, invoke with test payload.
- ECS / ECR: view clusters, services, and container images.
- CloudWatch / Logs: view metrics and log groups; tail logs.
- IAM / KMS / Secrets Manager: inspect identities, keys, and secrets (read-only or manage if permitted).
- Athena / Glue: run queries and view job/catalog info.
- EventBridge / Step Functions: inspect event buses and state machines, run test events.
- RDS / ElastiCache: view instances, endpoints and basic metrics.
- Route53 / ELB: view DNS records and load balancers.

Note: UI actions depend on configured credentials and permissions. Many screens offer a toolbar with common actions (Create, Refresh, Delete, Invoke, Query).

## 4. Run locally
Prerequisites:
- .NET 9 SDK
- Visual Studio 2022/2026 (or VS Code) or CLI
- Docker (optional, for LocalStack)

Configure:
1. Edit `appsettings.json` (or use environment vars) under the `Floci` section:
   - `ServiceUrl` (default: `http://localhost:4566` for LocalStack)
   - `Region`, `AccessKey`, `SecretKey`
2. Start LocalStack (optional):
   - docker: `docker run --rm -it -p 4566:4566 localstack/localstack`
   - or use your AWS account endpoint and credentials.

Run app:
- From PowerShell (project folder `FlociDashboard`):
  - `dotnet restore`
  - `dotnet run`
- From Visual Studio:
  - Open the solution, set the web project as startup, press F5 or Ctrl+F5.

Access:
- Open http://localhost:5000 (or the URL shown in the console) and navigate to the Services pages.

Troubleshooting:
- If using LocalStack, ensure `Floci:ServiceUrl` points to LocalStack (default `http://localhost:4566`) and credentials are set to `test`/`test` (or match your setup).
- Check logs in the application output window or console for errors.

License and contribution
- See project LICENSE and CONTRIBUTING files (if present) for contribution guidelines.
