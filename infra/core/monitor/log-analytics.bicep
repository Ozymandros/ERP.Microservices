@description('Name of the Log Analytics Workspace')
param name string

@description('Location for the Log Analytics Workspace')
param location string = resourceGroup().location

@description('Tags to apply to the Log Analytics Workspace')
param tags object = {}

// ...existing code...

@description('Log Analytics Workspace SKU')
param sku string

@description('Data retention in days')
param retentionInDays int

// For Free SKU, Azure only allows 30 days retention. For other SKUs, use minimum allowed or configured value.
var effectiveRetention = sku == 'Free' ? 30 : retentionInDays

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: effectiveRetention
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output id string = logAnalytics.id
output name string = logAnalytics.name
output customerId string = logAnalytics.properties.customerId
