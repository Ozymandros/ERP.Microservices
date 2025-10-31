@description('Name of the storage account')
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' existing = {
  name: storageAccountName
}

@description('Primary access key for the specified storage account')
@secure()
output primaryKey string = storageAccount.listKeys().keys[0].value
