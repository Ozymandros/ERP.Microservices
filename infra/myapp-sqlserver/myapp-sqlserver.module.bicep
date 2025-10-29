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
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': sqlServerName
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: myapp_sqlserver
}

// CREATE ALL REQUESTED DATABASES
resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-08-01' = [for dbName in databaseNames: {
  name: dbName
  parent: myapp_sqlserver
  location: location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  tags: {
    'aspire-resource-name': dbName
  }
}]

output sqlServerFqdn string = myapp_sqlserver.properties.fullyQualifiedDomainName

output name string = myapp_sqlserver.name

output sqlServerAdminName string = sqlServerAdminManagedIdentity.name

output databaseNames array = [for i in range(0, length(databaseNames)): sqlDatabases[i].name]
