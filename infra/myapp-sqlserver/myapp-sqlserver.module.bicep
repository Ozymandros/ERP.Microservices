import { sqlFirewallRuleName, sqlTlsVersion, databaseCollation, databaseMaxSizeBytes, databaseSkuName, databaseSkuTier, sqlServerlessSkuName, sqlServerlessSkuTier, sqlServerlessMinCapacity, sqlServerlessAutoPauseDelayMinutes, sqlServerlessUseFreeLimit, sqlServerlessFreeLimitExhaustionBehavior } from '../config/constants.bicep'

@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@description('SQL Server resource name')
param sqlServerName string

@description('User-assigned managed identity name for SQL administration')
param sqlAdminIdentityName string

@description('List of database names to create')
param databaseNames array

resource sqlServerAdminManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: sqlAdminIdentityName
  location: location
}

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: sqlServerAdminManagedIdentity.name
      sid: sqlServerAdminManagedIdentity.properties.principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: sqlTlsVersion
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': sqlServerName
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  name: sqlFirewallRuleName
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: myapp_sqlserver
}

// CREATE ALL REQUESTED DATABASES
// OPTIMITZACIÓ FINOPS: Serverless amb Free Tier per cost mínim
resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-08-01' = [for dbName in databaseNames: {
  name: dbName
  parent: myapp_sqlserver
  location: location
  properties: {
    collation: databaseCollation
    maxSizeBytes: databaseMaxSizeBytes
    catalogCollation: databaseCollation
    useFreeLimit: sqlServerlessUseFreeLimit  // CRÍTIC: Activa Free Tier (100k segons gratis)
    freeLimitExhaustionBehavior: sqlServerlessFreeLimitExhaustionBehavior  // Pausa automàtica quan s'esgota el límit gratuït
    minCapacity: json(sqlServerlessMinCapacity)  // Mínim serverless (0.5 vCores)
    autoPauseDelay: sqlServerlessAutoPauseDelayMinutes  // Pausa després de 60 minuts d'inactivitat (mínim permès)
  }
  sku: {
    name: sqlServerlessSkuName  // Serverless General Purpose Gen5
    tier: sqlServerlessSkuTier
    capacity: 1  // Capacitat base
  }
  tags: {
    'aspire-resource-name': dbName
  }
}]

output sqlServerFqdn string = myapp_sqlserver.properties.fullyQualifiedDomainName

output name string = myapp_sqlserver.name

output sqlServerAdminName string = sqlServerAdminManagedIdentity.name

output databaseNames array = [for i in range(0, length(databaseNames)): sqlDatabases[i].name]
