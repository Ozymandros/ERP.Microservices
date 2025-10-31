// ============================================================================
// Centralized Configuration Constants
// ============================================================================
// This module defines all hardcoded values used across the infrastructure.
// All configuration should flow through here to ensure consistency and
// easy updates across the entire solution.
// ============================================================================

// SQL Configuration
@export()
var sqlAdminUsername = 'sqladmin'

@export()
var sqlFirewallRuleName = 'AllowAllAzureIps'

@export()
var sqlFirewallStartIp = '0.0.0.0'

@export()
var sqlFirewallEndIp = '0.0.0.0'

@export()
var sqlTlsVersion = '1.2'

// Container Apps Configuration
@export()
var workloadProfileType = 'Consumption'

@export()
var workloadProfileName = 'consumption'

@export()
var aspireDashboardComponentName = 'aspire-dashboard'

@export()
var aspireDashboardComponentType = 'AspireDashboard'

// Container App Configuration
@export()
var defaultHealthCheckPath = '/health'

@export()
var defaultHealthCheckPort = 8080

@export()
var defaultHttpPort = 8080

@export()
var defaultMinReplicas = 2

@export()
var defaultMaxReplicas = 5

@export()
var defaultCpuCores = '0.5'

@export()
var defaultMemory = '1.0Gi'

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
var azureRoleIdAcrPull = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

@export()
var azureRoleIdAppConfigurationDataReader = '516239f1-63e1-4108-9233-9e7f68e97ce3'

@export()
var azureRoleIdKeyVaultSecretsUser = '4633458b-17de-408a-b874-0445c86d0e6e'

@export()
var azureRoleIdSqlDbContributor = '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec'

// Redis Configuration
@export()
var redisSkuBasic = 'Basic'

@export()
var redisFamily = 'C'

@export()
var redisCapacityBasic = 0

@export()
var redisTlsVersion = '1.2'

@export()
var redisPort = 6380

@export()
var redisEnableSsl = true

// Key Vault Configuration
@export()
var keyVaultSkuFamily = 'A'

@export()
var keyVaultSkuName = 'standard'

// Storage Configuration
@export()
var storageKind = 'StorageV2'

@export()
var storageSkuName = 'Standard_LRS'

@export()
var storageFileShareQuota = 1024

// Database Configuration
@export()
var databaseCollation = 'SQL_Latin1_General_CP1_CI_AS'

@export()
var databaseMaxSizeBytes = 2147483648

@export()
var databaseSkuName = 'Basic'

@export()
var databaseSkuTier = 'Basic'

// Log Analytics Configuration
@export()
var logAnalyticsSkuName = 'PerGB2018'

// Container Apps Environment Configuration
@export()
var containerAppEnvironmentStorageAccessKey = 'storagekey'

@export()
var containerAppEnvironmentStorageAccountName = 'caestorage'

// Application Insights Configuration
@export()
var applicationInsightsKind = 'web'

// SQL Server Configuration
@export()
var sqlServerVersion = '12.0'

// Container Registry Configuration
@export()
var containerRegistrySku = 'Basic'

// API Gateway Configuration (scaled higher than microservices)
@export()
var apiGatewayMinReplicas = 2

@export()
var apiGatewayMaxReplicas = 10

@export()
var apiGatewayCpuCores = '1.0'

// Service Health Check Configuration
@export()
var healthCheckHealthyThreshold = 1

@export()
var healthCheckUnhealthyThreshold = 3

@export()
var healthCheckIntervalSeconds = 5

@export()
var healthCheckTimeoutSeconds = 5

// Redis Maxmemory Policy
@export()
var redisMaxmemoryPolicy = 'allkeys-lru'

// Storage Configuration
@export()
var storageFileServiceName = 'default'

@export()
var storageDefaultFileSku = 'Hot'

// Tags
@export()
var tagAzureDevEnvironment = 'azd-env-name'

@export()
var tagAspireNamePrefix = 'aspire-name-prefix'

@export()
var tagAspireResourceName = 'aspire-resource-name'

@export()
var tagLogAnalyticsWorkspaceId = 'log-analytics-workspace-id'

@export()
var tagManagedIdentityPrincipalId = 'managed-identity-principal-id'

@export()
var tagEnvironmentName = 'environment-name'

