@description('Name of the SQL Server')
param name string

@description('Location for the SQL Server')
param location string = resourceGroup().location

@description('Tags to apply to the SQL Server')
param tags object = {}

@description('SQL Server administrator login')
param administratorLogin string

@description('SQL Server administrator password')
@secure()
param administratorLoginPassword string

@description('Array of database names to create')
param databases array = []

@description('Minimum TLS version')
param minimalTlsVersion string = '1.2'

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    minimalTlsVersion: minimalTlsVersion
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access the server
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Create databases
resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-05-01-preview' = [for db in databases: {
  name: db.name
  parent: sqlServer
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
    isLedgerOn: false
  }
}]

output id string = sqlServer.id
output name string = sqlServer.name
output fqdn string = sqlServer.properties.fullyQualifiedDomainName
