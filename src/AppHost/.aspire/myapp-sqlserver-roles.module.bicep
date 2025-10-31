@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param myapp_sqlserver_outputs_name string

param myapp_sqlserver_outputs_sqlserveradminname string

param principalId string

param principalName string

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: myapp_sqlserver_outputs_sqlserveradminname
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: principalName
}