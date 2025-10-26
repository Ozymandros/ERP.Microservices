@description('Name of the Key Vault')
param name string

@description('Location for Key Vault')
param location string = resourceGroup().location

@description('Tags to apply')
param tags object = {}

@description('Redis host name')
param redisHostName string

@description('Redis primary key')
@secure()
param redisPrimaryKey string

@description('SQL Server FQDN')
param sqlFqdn string

@description('SQL admin password')
@secure()
param sqlAdminPassword string

@description('JWT secret value')
@secure()
param jwtSecretKey string

@description('Enable creation of Key Vault and secrets')
param enableKeyVault bool = false

// Key Vault

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = if (enableKeyVault) {
  name: name
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    enabledForTemplateDeployment: true
  }
  tags: tags
}

// Redis secret
resource kvRedisSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'redis-connection'
  properties: {
    value: '${redisHostName}:6380,password=${redisPrimaryKey},ssl=True,abortConnect=False'
  }
}

// JWT secret
resource kvJwtSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

// SQL DB secrets
resource kvSqlSecretAuth 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-authdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=AuthDB;User Id=sqladmin;Password=${sqlAdminPassword};'
  }
}

resource kvSqlSecretBilling 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-billingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=BillingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretInventory 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-inventorydb'
  properties: {
    value: 'Server=${sqlFqdn};Database=InventoryDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretOrders 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-ordersdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=OrderDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretPurchasing 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-purchasingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=PurchasingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretSales 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'sql-connection-salesdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=SalesDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

output keyVaultId string = enableKeyVault ? resourceId('Microsoft.KeyVault/vaults', name) : ''
output keyVaultUri string = enableKeyVault ? 'https://${name}.${environment().suffixes.keyvaultDns}' : ''

// Return secret NAMES (not values) so callers can reference secret names safely
output redisSecretName string = enableKeyVault ? kvRedisSecret.name : ''
output jwtSecretName string = enableKeyVault ? kvJwtSecret.name : ''
output sqlAuthSecretName string = enableKeyVault ? kvSqlSecretAuth.name : ''
output sqlBillingSecretName string = enableKeyVault ? kvSqlSecretBilling.name : ''
output sqlInventorySecretName string = enableKeyVault ? kvSqlSecretInventory.name : ''
output sqlOrdersSecretName string = enableKeyVault ? kvSqlSecretOrders.name : ''
output sqlPurchasingSecretName string = enableKeyVault ? kvSqlSecretPurchasing.name : ''
output sqlSalesSecretName string = enableKeyVault ? kvSqlSecretSales.name : ''
