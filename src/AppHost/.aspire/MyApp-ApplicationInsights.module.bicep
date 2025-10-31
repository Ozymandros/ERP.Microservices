@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param myapp_loganalyticsworkspace_outputs_loganalyticsworkspaceid string

resource MyApp_ApplicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('MyApp_ApplicationInsights-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: myapp_loganalyticsworkspace_outputs_loganalyticsworkspaceid
  }
  tags: {
    'aspire-resource-name': 'MyApp-ApplicationInsights'
  }
}

output appInsightsConnectionString string = MyApp_ApplicationInsights.properties.ConnectionString

output name string = MyApp_ApplicationInsights.name