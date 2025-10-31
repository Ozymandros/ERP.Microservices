@description('Log Analytics workspace name. Defaults to the shared workspace provisioned in resources.bicep.')
param logAnalyticsWorkspaceName string = 'law-${uniqueString(resourceGroup().id)}'

resource MyApp_LogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

output logAnalyticsWorkspaceId string = MyApp_LogAnalyticsWorkspace.id

output name string = MyApp_LogAnalyticsWorkspace.name
