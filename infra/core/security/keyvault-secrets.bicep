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

@description('Redis cache password for authentication')
@secure()
param redisCachePassword string = ''

@description('SQL Server FQDN')
param sqlFqdn string

@description('SQL admin password')
@secure()
param sqlAdminPassword string

@description('Database name for the Auth service')
param authDbName string

@description('Database name for the Billing service')
param billingDbName string

@description('Database name for the Inventory service')
param inventoryDbName string

@description('Database name for the Orders service')
param ordersDbName string

@description('Database name for the Purchasing service')
param purchasingDbName string

@description('Database name for the Sales service')
param salesDbName string

@description('JWT secret value')
@secure()
param jwtSecretKey string

// Key Vault

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
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
resource kvRedisSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'redis-connection'
  properties: {
    value: '${redisHostName}:6380,password=${redisPrimaryKey},ssl=True,abortConnect=False'
  }
}

// ✅ Redis cache password secret (for authentication)
resource kvRedisCachePasswordSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (!empty(redisCachePassword)) {
  parent: keyVault
  name: 'redis-cache-password'
  properties: {
    value: redisCachePassword
  }
}

// JWT secret
resource kvJwtSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

// SQL DB secrets
resource kvSqlSecretAuth 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-authdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${authDbName};User Id=sqladmin;Password=${sqlAdminPassword};'
  }
}

resource kvSqlSecretBilling 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-billingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${billingDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretInventory 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-inventorydb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${inventoryDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretOrders 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-ordersdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${ordersDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretPurchasing 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-purchasingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${purchasingDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretSales 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-salesdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${salesDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultName string = keyVault.name

// Return secret NAMES (not values) so callers can reference secret names safely
output redisSecretName string = kvRedisSecret.name
output redisAuthSecretName string = 'redis-cache-password'
output jwtSecretName string = kvJwtSecret.name
output sqlAuthSecretName string = kvSqlSecretAuth.name
output sqlBillingSecretName string = kvSqlSecretBilling.name
output sqlInventorySecretName string = kvSqlSecretInventory.name
output sqlOrdersSecretName string = kvSqlSecretOrders.name
output sqlPurchasingSecretName string = kvSqlSecretPurchasing.name
output sqlSalesSecretName string = kvSqlSecretSales.name
