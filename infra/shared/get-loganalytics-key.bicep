@description('Name of the Log Analytics workspace')
param workspaceName string

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: workspaceName
}

@description('Primary shared key for the specified Log Analytics workspace')
@secure()
output primarySharedKey string = workspace.listKeys().primarySharedKey
