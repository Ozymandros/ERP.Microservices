@description('Name of the Log Analytics Workspace')
param name string

@description('Location for the Log Analytics Workspace')
param location string = resourceGroup().location

@description('Tags to apply to the Log Analytics Workspace')
param tags object = {}

import { logAnalyticsSkuName, finopsLogAnalyticsRetentionDays } from '../../config/constants.bicep'

@description('Log Analytics Workspace SKU')
param sku string = logAnalyticsSkuName

@description('Data retention in days')
param retentionInDays int = finopsLogAnalyticsRetentionDays  // OPTIMITZACIÓ FINOPS: Mínim possible per dev no utilitzat

var effectiveRetention = sku == 'Free' ? 7 : retentionInDays

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
