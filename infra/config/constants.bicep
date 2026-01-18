// ============================================================================
// Centralized Configuration Constants
// ============================================================================
// This module defines all hardcoded values used across the infrastructure.
// All configuration should flow through here to ensure consistency and
// easy updates across the entire solution.
// ============================================================================

// SQL Configuration
@export()
@description('SQL Server administrator username for authentication')
var sqlAdminUsername = 'sqladmin'

@export()
@description('Name of the firewall rule that allows all Azure IP addresses to access SQL Server')
var sqlFirewallRuleName = 'AllowAllAzureIps'

@export()
@description('Starting IP address for SQL Server firewall rule (0.0.0.0 allows all Azure services)')
var sqlFirewallStartIp = '0.0.0.0'

@export()
@description('Ending IP address for SQL Server firewall rule (0.0.0.0 allows all Azure services)')
var sqlFirewallEndIp = '0.0.0.0'

@export()
@description('Minimum TLS version required for SQL Server connections')
var sqlTlsVersion = '1.2'

// Container Apps Configuration
@export()
@description('Workload profile type for Container Apps Environment (Consumption = pay-per-use, Dedicated = fixed capacity)')
var workloadProfileType = 'Consumption'

@export()
@description('Workload profile name (lowercase) used to match the profile type in Container Apps')
var workloadProfileName = 'consumption'

@export()
@description('Component name for Aspire dashboard integration (if using .NET Aspire)')
var aspireDashboardComponentName = 'aspire-dashboard'

@export()
@description('Component type identifier for Aspire dashboard in Container Apps')
var aspireDashboardComponentType = 'AspireDashboard'

// Container App Configuration
@export()
@description('Default HTTP path for health check probes (liveness and readiness)')
var defaultHealthCheckPath = '/health'

@export()
@description('Default TCP port number for health check probes')
var defaultHealthCheckPort = 8080

@export()
@description('Default HTTP port exposed by container applications')
var defaultHttpPort = 8080

@export()
@description('Default minimum number of container replicas for auto-scaling (ensures availability)')
var defaultMinReplicas = 2

@export()
@description('Default maximum number of container replicas for auto-scaling (cost control)')
var defaultMaxReplicas = 5

@export()
@description('Default CPU allocation per container replica (0.5 cores = 500m)')
var defaultCpuCores = 1

@export()
@description('Default memory allocation per container replica (1.0Gi = 1024 MiB)')
var defaultMemory = '1.0Gi'

// FinOps Configuration - Cost Optimization for Non-Production Environments
@export()
@description('FinOps: Minimum replicas for scale-to-zero (0 = no cost when idle)')
var finopsMinReplicas = 0

@export()
@description('FinOps: Maximum replicas for cost control (1 = minimal scaling)')
var finopsMaxReplicas = 1

@export()
@description('FinOps: Minimum CPU allocation per container replica (0.25 cores = 250m)')
var finopsCpuCores = 1

@export()
@description('FinOps: Minimum memory allocation per container replica (0.5Gi = 512 MiB)')
var finopsMemory = '0.5Gi'

// ============================================================================
// NOTE: Azure Role Definition IDs are NO LONGER hardcoded here.
// They are now passed as deployment parameters for better flexibility and
// validation. This prevents issues with wrong role IDs and ensures compatibility
// across different Azure environments.
// See main.bicep for role definition ID parameters.
// ============================================================================

// ============================================================================
// Azure Built-in Role IDs (Microsoft-documented stable constants)
// Reference: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
// ============================================================================
// These are the well-known IDs for Azure's built-in roles. They never change
// and are used in Bicep `existing` resources to reference built-in role definitions.

@export()
@description('Azure built-in role ID: App Configuration Data Reader - Allows reading App Configuration data')
var azureRoleIdAppConfigurationDataReader = '516239f1-63e1-4d78-a4de-a74fb236a071'

@export()
@description('Azure built-in role ID: Key Vault Secrets User - Allows reading secret values from Key Vault')
var azureRoleIdKeyVaultSecretsUser = '4633458b-17de-408a-b874-0445c86b69e6'

@export()
@description('Azure built-in role ID: SQL DB Contributor - Allows managing SQL databases (not server-level)')
var azureRoleIdSqlDbContributor = '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec'

// Redis Configuration
@export()
@description('Azure Cache for Redis SKU tier (Basic = lowest cost, no SLA)')
var redisSkuBasic = 'Basic'

@export()
@description('Redis cache family (C = standard performance tier)')
var redisFamily = 'C'

@export()
@description('Redis cache capacity in GB (0 = Basic tier minimum)')
var redisCapacityBasic = 0

@export()
@description('Minimum TLS version for Redis connections (1.2 = secure)')
var redisTlsVersion = '1.2'

@export()
@description('Redis SSL/TLS port number (6380 = standard secure port)')
var redisPort = 6380

@export()
@description('Enable SSL/TLS encryption for Redis connections (required for production)')
var redisEnableSsl = true

// Key Vault Configuration
@export()
@description('Key Vault SKU family (A = standard family)')
var keyVaultSkuFamily = 'A'

@export()
@description('Key Vault SKU name (standard = includes soft delete and purge protection)')
var keyVaultSkuName = 'standard'

// Storage Configuration
@export()
@description('Azure Storage account kind (StorageV2 = general-purpose v2 with latest features)')
var storageKind = 'StorageV2'

@export()
@description('Storage account SKU (Standard_LRS = locally redundant storage, lowest cost)')
var storageSkuName = 'Standard_LRS'

@export()
@description('Azure Files share quota in GB (1024 = 1 TB maximum for standard shares)')
var storageFileShareQuota = 1024

// Database Configuration
@export()
@description('SQL Server database collation (case-insensitive, accent-sensitive, Latin1)')
var databaseCollation = 'SQL_Latin1_General_CP1_CI_AS'

// @export()
// @description('Maximum database size in bytes (2147483648 = 2 GB, Basic tier limit)')
// var databaseMaxSizeBytes = 2147483648

@export()
@description('SQL database SKU name (Basic = lowest cost tier, suitable for dev/test)')
var databaseSkuName = 'Basic'

@export()
@description('SQL database service tier (Basic = entry-level, no SLA)')
var databaseSkuTier = 'Basic'

// SQL Serverless Configuration (FinOps)
@export()
@description('FinOps: SQL Serverless SKU name (GP_S_Gen5 = General Purpose Serverless Gen5)')
var sqlServerlessSkuName = 'GP_S_Gen5'

@export()
@description('FinOps: SQL Serverless service tier (GeneralPurpose = serverless compute)')
var sqlServerlessSkuTier = 'GeneralPurpose'

@export()
@description('FinOps: SQL Serverless minimum capacity in vCores (0.5 = minimum allowed)')
var sqlServerlessMinCapacity = '1'

@export()
@description('FinOps: SQL Serverless auto-pause delay in minutes (60 = minimum allowed)')
var sqlServerlessAutoPauseDelayMinutes = 60

@export()
@description('FinOps: SQL Serverless free tier enabled (true = 100k seconds/month free)')
var sqlServerlessUseFreeLimit = true

@export()
@description('FinOps: SQL Serverless free limit exhaustion behavior (AutoPause = pause when limit reached)')
var sqlServerlessFreeLimitExhaustionBehavior = 'AutoPause'

// Log Analytics Configuration
@export()
@description('Log Analytics workspace pricing model (PerGB2018 = pay per GB ingested)')
var logAnalyticsSkuName = 'PerGB2018'

@export()
@description('FinOps: Log Analytics retention in days (30 = Free SKU minimum allowed by Azure)')
var finopsLogAnalyticsRetentionDays = 30

// Container Apps Environment Configuration
@export()
@description('Access key name for Container Apps Environment storage volumes')
var containerAppEnvironmentStorageAccessKey = 'storagekey'

@export()
@description('Storage account name prefix for Container Apps Environment persistent volumes')
var containerAppEnvironmentStorageAccountName = 'caestorage'

// Application Insights Configuration
@export()
@description('Application Insights resource kind (web = standard web application telemetry)')
var applicationInsightsKind = 'web'

// SQL Server Configuration
@export()
@description('SQL Server version (12.0 = SQL Server 2014, compatible with most features)')
var sqlServerVersion = '12.0'

// ============================================================================
// Container Registry - REMOVED FOR FINOPS (Using GHCR instead)
// ============================================================================
// FINOPS OPTIMIZATION: ACR constants removed as GHCR (GitHub Container Registry)
// is used instead, saving ~€4.64/month. GHCR provides free image hosting.

// API Gateway Configuration (scaled higher than microservices)
@export()
@description('Minimum replicas for API Gateway (higher than microservices due to higher traffic)')
var apiGatewayMinReplicas = 2

@export()
@description('Maximum replicas for API Gateway (allows scaling to handle traffic spikes)')
var apiGatewayMaxReplicas = 10

@export()
@description('CPU allocation for API Gateway (1.0 cores = higher than microservices for better performance)')
var apiGatewayCpuCores = '1.0'

// Service Health Check Configuration
@export()
@description('Number of consecutive successful health checks required to mark container as healthy')
var healthCheckHealthyThreshold = 1

@export()
@description('Number of consecutive failed health checks before marking container as unhealthy')
var healthCheckUnhealthyThreshold = 3

@export()
@description('Interval between health check probes in seconds')
var healthCheckIntervalSeconds = 5

@export()
@description('Timeout for health check probe in seconds (must be less than interval)')
var healthCheckTimeoutSeconds = 5

// Redis Maxmemory Policy
@export()
@description('Redis eviction policy when memory limit is reached (allkeys-lru = least recently used)')
var redisMaxmemoryPolicy = 'allkeys-lru'

// Storage Configuration
@export()
@description('Azure Files service name within storage account (default = standard file service)')
var storageFileServiceName = 'default'

@export()
@description('Azure Files share tier (Hot = frequently accessed files, lower latency)')
var storageDefaultFileSku = 'Hot'

@export()
@description('FinOps: Storage account access tier (Hot = frequently accessed, lower latency)')
var finopsStorageAccessTier = 'Hot'

@export()
@description('FinOps: Container Apps Environment zone redundancy (false = cost optimization)')
var finopsZoneRedundant = false

@export()
@description('FinOps: Azure Files share quota in GB (1 = minimum allowed, maximum cost optimization)')
var finopsStorageFileShareQuota = 1

@export()
@description('FinOps: Application Insights sampling percentage (20 = 20%, reduces telemetry costs by 80%)')
var finopsApplicationInsightsSamplingPercentage = 20

@export()
@description('FinOps: App Configuration SKU (Free = no cost, suitable for dev/test)')
var finopsAppConfigSku = 'free'

@export()
@description('FinOps: API Gateway minimum replicas for dev/test (0 = scale-to-zero, no cost when idle)')
var finopsApiGatewayMinReplicas = 0

@export()
@description('FinOps: API Gateway CPU allocation for dev/test (0.25 cores = minimal resources)')
var finopsApiGatewayCpuCores = '0.25'

// Tags
@export()
@description('Tag key for Azure Developer CLI environment name (used for resource organization)')
var tagAzureDevEnvironment = 'azd-env-name'

@export()
@description('Tag key for .NET Aspire name prefix (if using Aspire orchestration)')
var tagAspireNamePrefix = 'aspire-name-prefix'

@export()
@description('Tag key for .NET Aspire resource name (if using Aspire orchestration)')
var tagAspireResourceName = 'aspire-resource-name'

@export()
@description('Tag key storing Log Analytics Workspace ID (for resource tracking and monitoring)')
var tagLogAnalyticsWorkspaceId = 'log-analytics-workspace-id'

@export()
@description('Tag key storing Managed Identity Principal ID (for RBAC and access tracking)')
var tagManagedIdentityPrincipalId = 'managed-identity-principal-id'

@export()
@description('Tag key for environment name (dev, staging, prod) for resource categorization')
var tagEnvironmentName = 'environment-name'
