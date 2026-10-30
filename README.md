# RubberDuck: a web API on Azure with Bicep and a CI/CD pipeline

A small ASP.NET Core web API ("rubber duck debugging"): it returns a random duck image and a bit of debugging wisdom.

> **The API is deliberately very simple.** The point of this project is everything around it: a **Bicep template** that creates the Azure infrastructure, a **Key Vault with a managed identity** for the secret, and an **Azure Pipeline** that builds, tests, scans and deploys the app. Use it as a template for your own project.

## What it does

| Endpoint | Description |
|---|---|
| `GET /health` | Returns the status of the app |
| `GET /api/duck` | Returns a random duck image URL (from [random-d.uk](https://random-d.uk)) and a quote (from [API Ninjas](https://api-ninjas.com), needs a free API key) |

## How it fits together

```
git push ──> Azure Pipeline ──> Build + test ──> SonarQube Cloud scan (main only)
                  │
                  └── main only: deploy ──> App Service ──(managed identity)──> Key Vault
                                                 │
                                         Azure Monitor alert ──> Action Group (email)
```

- **Bicep** creates the Key Vault, the App Service Plan (Linux), the web app with a system-assigned managed identity, and the role assignment *Key Vault Secrets User* for that identity.
- The **API key is never in the code or in Git.** It is stored as a Key Vault secret and read by the app through its managed identity.
- The **pipeline** builds and tests every push on every branch. Pushes to `main` are also scanned and deployed.

## Repository layout

```
.
├── azure-pipelines.yml     # build, test, scan, deploy
├── infra/
│   ├── main.bicep          # the infrastructure template
│   └── main.bicepparam     # example parameters
├── src/RubberDuck/         # the web API (and RubberDuck.http with example requests)
└── tests/                  # unit tests
```

## Prerequisites

- An Azure subscription, and permission to create role assignments in your resource group (for example Owner)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) with Bicep (`az bicep install`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure DevOps organization and project (for the pipeline)
- A free API key from [API Ninjas](https://api-ninjas.com)
- Optional: a SonarQube Cloud account (see below)

## Run locally

```
cd src/RubberDuck
dotnet user-secrets set "ApiNinjas:Key" "<your-api-key>"
dotnet run
```

User secrets are stored on your computer, outside the project folder, so the key never ends up in Git. Open `/health` and `/api/duck` in the browser, or use `src/RubberDuck/RubberDuck.http`. Run the tests with `dotnet test`.

## 1. Deploy the infrastructure

```
az login
az group create --name <your-resource-group> --location <region>
az deployment group create --resource-group <your-resource-group> --parameters infra/main.bicepparam
```

Edit `infra/main.bicepparam` first if you want other names or another pricing tier. The deployment prints its outputs when it finishes: `keyVaultName`, `keyVaultUri`, `webAppName` and `webAppUrl`. You need `keyVaultName` and `webAppName` below.

## 2. Store the API key in Key Vault

The vault uses Azure RBAC, so you need a role that allows you to create secrets. Give it to yourself first:

```
az role assignment create --role "Key Vault Secrets Officer" \
  --assignee <your-user-principal-name> \
  --scope $(az keyvault show --name <keyVaultName> --query id -o tsv)

az keyvault secret set --vault-name <keyVaultName> --name "ApiNinjas--Key" --value "<your-api-key>"
```

The secret is named `ApiNinjas--Key`. The configuration system turns the double dash into `:`, so the app reads it as `ApiNinjas:Key`, the same key as in the local user secrets. In Azure, the web app gets the vault address from the `KeyVaultUri` app setting (set by the Bicep template) and reads the secret with `DefaultAzureCredential`, which uses the web app's managed identity. Role assignments can take a few minutes to take effect.

## 3. Set up the pipeline

1. **Create a pipeline** in Azure DevOps from `azure-pipelines.yml`. Azure Pipelines can build a repository on GitHub through the Azure Pipelines GitHub app. Alternatively, import the repo into Azure Repos.
2. **Create a service connection to Azure:** Project settings → Service connections → New → Azure Resource Manager. Name it `azure-rubberduck`, or use another name and change `azureSubscription` in the YAML. Scope it to your resource group.
   - If creating it automatically fails with *Insufficient privileges* (common if you aren't an admin in the Entra tenant), create the connection manually using a managed identity. Give that identity a role that allows deploying to the web app (for example Contributor on the resource group).
3. **Replace the placeholders** in `azure-pipelines.yml`: set `webAppName` to the `webAppName` output from step 1.
4. **Push to `main`.** The pipeline builds, tests and deploys. Open `<webAppUrl>/health` to check.

Pipeline behavior:

| Trigger | Build + test | SonarQube scan | Deploy |
|---|---|---|---|
| Push to any branch | yes | no | no |
| Push or merge to `main` | yes | yes | yes |

## Code scanning with SonarQube Cloud

The pipeline can scan the code with [SonarQube Cloud](https://sonarcloud.io) (free plan). The scan runs on pushes to `main`. On the free plan, only `main` and pull requests to `main` can be analyzed.

### One-time setup

1. **Create a SonarQube Cloud account** and an organization. Note the organization key.
2. **Import your repository** as a project, a .NET project in this case. Note the project key.
3. **Install the SonarQube Cloud extension** in your Azure DevOps organization (Marketplace). The `SonarCloud*` pipeline tasks come from it.
4. **Create a token** in SonarQube Cloud (My Account → Security).
5. **Create a service connection** in Azure DevOps: Project settings → Service connections → New service connection → SonarQube Cloud. Paste the token and name it `sonarqube-cloud`, or use another name and change it in the YAML.
6. **Edit `azure-pipelines.yml`:** replace `<your-sonar-organization>` and `<your-sonar-project-key>` with your own values.

### How it runs in the pipeline

```yaml
- task: SonarCloudPrepare@4.2.6    # before the build
- task: DotNetCoreCLI@2            # build
- task: DotNetCoreCLI@2            # tests
- task: SonarCloudAnalyze@4.2.6    # after build and tests
- task: SonarCloudPublish@4.2.6    # waits for the quality gate result
```

- **Prepare** has to run before the build, so the scanner can follow the compilation.
- **Analyze** runs after the build and the tests. There is no point scanning code that doesn't compile or fails its tests; those errors should be fixed first.
- **Publish** waits up to 300 seconds for the quality gate result and shows it in the pipeline run.
- The pipeline checks out the full history (`fetchDepth: 0`), which the scanner needs to detect new code.
- The tasks are pinned to a version (`@4.2.6`) so a new release can't change your build unexpectedly.

### Reading the result

Open your project on SonarQube Cloud to see the quality gate, the ratings (security, reliability, maintainability) and each issue with an explanation and a suggested fix.

## Monitoring

Monitoring was set up in the Azure portal and is **not** part of the Bicep template.

- **Metrics:** in the web app, open Monitoring → Metrics to see Requests and Response Time.
- **Alert rule:** a metric alert on the web app for **Http 5xx** (total > 0, 5-minute look-back, evaluated every minute).
- **Action group:** sends an email to the people you add.

To test it, disable the `ApiNinjas--Key` secret in Key Vault and call `/api/duck`. The app can no longer read the key, returns server errors, and the alert fires. Then enable the secret again and restart the web app. The `/health` endpoint is a useful check along the way.

## Cost and cleanup

The template uses the free `F1` App Service tier, but Key Vault and alerts can still cost a little. Delete everything when you're done:

```
az group delete --name <your-resource-group> --yes
az keyvault purge --name <keyVaultName> --location <region>
```

The purge is needed because Key Vault keeps deleted vaults for the soft-delete period (7 days in this template) and reserves the name. Also delete the Azure service connection and the Sonarqube token if you no longer need them.

## Limitations

- The API has no authentication, because it is a demo.
- SonarQube analysis of pull requests isn't set up. The free plan allows it, but it needs a build validation policy in Azure DevOps.
- The monitoring isn't in Bicep.
