param myapp_sqlserver_outputs_name string

param principalName string

@description('Principal ID of the managed identity requiring SQL access')
param principalId string

@description('Role Definition GUID - SQL DB Contributor role GUID (36 characters)')
@minLength(36)
@maxLength(36)
param roleGuid string

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}

var managedIdentityId = resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', principalName)

resource sqlDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(myapp_sqlserver.id, managedIdentityId, 'sql-db-contributor')
  scope: myapp_sqlserver
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleGuid)
  }
}
