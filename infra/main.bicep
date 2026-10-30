@description('Short prefix for all resource names (3-7 lowercase letters or digits). Key Vault names are limited to 24 characters.')
@minLength(3)
@maxLength(7)
param baseName string = 'duckapi'

@description('Azure region. Defaults to the location of the resource group.')
param location string = resourceGroup().location

@description('App Service Plan pricing tier. F1 is free but has quotas.')
@allowed([
  'F1'
  'B1'
  'S1'
])
param appServiceSku string = 'F1'

@description('Runtime stack for the Linux web app.')
param linuxFxVersion string = 'DOTNETCORE|10.0'

var uniqueSuffix = uniqueString(resourceGroup().id)
var keyVaultName = 'kv-${baseName}-${uniqueSuffix}'
var appServicePlanName = 'asp-${baseName}'
var webAppName = 'app-${baseName}-${uniqueSuffix}'

// Built-in Azure role "Key Vault Secrets User" (same ID in every tenant)
var keyVaultSecretUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

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
    name: appServiceSku
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
      linuxFxVersion: linuxFxVersion
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
      ]
    }
  }
}

resource webAppSecretsAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.id, keyVaultSecretUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretUserRoleId)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
