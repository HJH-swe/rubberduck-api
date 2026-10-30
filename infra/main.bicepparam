// Example parameters for main.bicep.
// Copy or edit this file, then deploy with:
//   az deployment group create --resource-group <your-resource-group> --parameters infra/main.bicepparam

// You can keep one file per environment (for example dev.bicepparam and prod.bicepparam)
// and use the same main.bicep for all of them.


using 'main.bicep'

// Prefix for all resource names (3-7 lowercase letters or digits).
// Example: 'duckapi' gives kv-duckapi-<hash>, asp-duckapi and app-duckapi-<hash>.
// Kept short because Key Vault names can be at most 24 characters.
param baseName = 'duckapi'

// Pricing tier of the App Service Plan: 'F1' (free, with quotas), 'B1' or 'S1'.
// F1 is enough to try the template. Pick B1 or S1 if you need more.
param appServiceSku = 'F1'

// Optional: uncomment to override the defaults.

// Azure region. The default is the region of the resource group.
// Example: 'swedencentral'
// param location = '<your-Azure-region>'

// Runtime stack of the web app. The default is .NET 10 on Linux.
// Example: 'DOTNETCORE|10.0'
// param linuxFxVersion = '<your-runtime-stack>'
