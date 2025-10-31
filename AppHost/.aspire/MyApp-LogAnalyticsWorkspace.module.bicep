@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource MyApp_LogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('MyAppLogAnalyticsWorkspace-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'MyApp-LogAnalyticsWorkspace'
  }
}

output logAnalyticsWorkspaceId string = MyApp_LogAnalyticsWorkspace.id

output name string = MyApp_LogAnalyticsWorkspace.name