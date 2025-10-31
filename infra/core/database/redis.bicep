import { redisTlsVersion, redisMaxmemoryPolicy } from '../../config/constants.bicep'

@description('Name of the Redis Cache')
param name string

@description('Location for the Redis Cache')
param location string = resourceGroup().location

@description('Tags to apply to the Redis Cache')
param tags object = {}

@description('Redis Cache SKU')
param sku string = 'Basic'

@description('Redis Cache family')
param family string = 'C'

@description('Redis Cache capacity')
param capacity int = 0

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
      family: family
      capacity: capacity
    }
    enableNonSslPort: false
    minimumTlsVersion: redisTlsVersion
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': redisMaxmemoryPolicy
      // ✅ Wire cache password for authentication
      // When requireauth is true, clients must authenticate with the password
      // requireauth: (!empty(cachePassword) ? 'true' : 'false')
    }
  }
}

output id string = redis.id
output name string = redis.name
output hostName string = redis.properties.hostName
output sslPort int = redis.properties.sslPort
@description('Redis primary key - should be stored as a secret')
@secure()
output primaryKey string = redis.listKeys().primaryKey
