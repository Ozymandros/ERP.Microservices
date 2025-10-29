param myapp_sqlserver_outputs_name string

param principalName string

@description('Principal ID of the managed identity requiring SQL access')
param principalId string

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}

var managedIdentityId = resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', principalName)

var sqlDbContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec')

resource sqlDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(myapp_sqlserver.id, managedIdentityId, 'sql-db-contributor')
  scope: myapp_sqlserver
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: sqlDbContributorRoleId
  }
}
