param location string = resourceGroup().location

var keyVaultName = 'kv-duckapi-${uniqueString(resourceGroup().id)}'
var appServicePlanName = 'asp-duckapi'
var webAppName = 'app-duckapi-${uniqueString(resourceGroup().id)}'

resource keyVault 'Microsoft.KeyVault/vaults@2026-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2025-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
  }
}

output keyVaultUri string = keyVault.properties.vaultUri
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
