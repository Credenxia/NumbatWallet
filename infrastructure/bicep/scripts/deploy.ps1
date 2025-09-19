# NumbatWallet Infrastructure Deployment Script
# Purpose: Deploy Azure infrastructure using Bicep templates

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,

    [Parameter(Mandatory=$false)]
    [string]$Subscription,

    [Parameter(Mandatory=$false)]
    [string]$Location = "australiaeast",

    [Parameter(Mandatory=$false)]
    [string]$AdminObjectId,

    [Parameter(Mandatory=$false)]
    [string]$PostgresAdminUsername = "nwadmin",

    [Parameter(Mandatory=$false)]
    [SecureString]$PostgresAdminPassword,

    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,

    [Parameter(Mandatory=$false)]
    [switch]$ValidateOnly,

    [Parameter(Mandatory=$false)]
    [switch]$Destroy
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Color functions
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Info { Write-Host $args -ForegroundColor Blue }

# Banner
Write-Info @"
╔══════════════════════════════════════════════════════╗
║     NumbatWallet Infrastructure Deployment Tool      ║
╚══════════════════════════════════════════════════════╝
"@

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BicepDir = Split-Path -Parent $ScriptDir
$MainBicep = Join-Path $BicepDir "main.bicep"
$ParamsFile = Join-Path $BicepDir "parameters" "$Environment.parameters.json"

# Validate files exist
if (-not (Test-Path $MainBicep)) {
    Write-Error "Error: main.bicep not found at $MainBicep"
    exit 1
}

if (-not (Test-Path $ParamsFile)) {
    Write-Error "Error: Parameter file not found at $ParamsFile"
    exit 1
}

# Check Azure CLI installation
try {
    $null = az version
} catch {
    Write-Error "Error: Azure CLI is not installed"
    Write-Host "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check Bicep CLI installation
try {
    $null = az bicep version
} catch {
    Write-Warning "Bicep CLI not found. Installing..."
    az bicep install
}

# Login check
Write-Info "Checking Azure login status..."
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Warning "Not logged in to Azure. Please login..."
    az login
    $account = az account show | ConvertFrom-Json
}

# Set subscription if provided
if ($Subscription) {
    Write-Info "Setting subscription to: $Subscription"
    az account set --subscription $Subscription
    $account = az account show | ConvertFrom-Json
}

Write-Success "Using subscription: $($account.name) ($($account.id))"

# Generate deployment name
$DeploymentName = "numbatwallet-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Handle admin object ID
if (-not $AdminObjectId) {
    Write-Warning "Admin object ID not provided. Attempting to use current user..."
    try {
        $currentUser = az ad signed-in-user show | ConvertFrom-Json
        $AdminObjectId = $currentUser.id
        Write-Success "Using current user object ID: $AdminObjectId"
    } catch {
        Write-Error "Error: Could not determine admin object ID. Please provide with -AdminObjectId parameter"
        exit 1
    }
}

# Handle PostgreSQL password
if (-not $PostgresAdminPassword -and -not $ValidateOnly -and -not $WhatIf) {
    Write-Warning "PostgreSQL admin password not provided."
    $PostgresAdminPassword = Read-Host "Enter PostgreSQL admin password" -AsSecureString

    if ($PostgresAdminPassword.Length -eq 0) {
        # Generate a secure password if not provided
        Add-Type -AssemblyName System.Web
        $plainPassword = [System.Web.Security.Membership]::GeneratePassword(32, 8)
        $PostgresAdminPassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force
        Write-Warning "Generated PostgreSQL password (save this securely): $plainPassword"
    }
}

# Convert SecureString to plain text for Azure CLI (temporary)
if ($PostgresAdminPassword) {
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($PostgresAdminPassword)
    $PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
} else {
    $PlainPassword = "temp-password-for-validation"
}

# Create temporary parameters file with values
$TempParams = [System.IO.Path]::GetTempFileName() + ".json"
$paramsContent = Get-Content $ParamsFile | ConvertFrom-Json
$paramsContent.parameters.administratorObjectId.value = $AdminObjectId
$paramsContent | ConvertTo-Json -Depth 10 | Set-Content $TempParams

try {
    # Deployment operations
    if ($Destroy) {
        Write-Error @"
╔══════════════════════════════════════════════════════╗
║                    ⚠️  WARNING ⚠️                      ║
║   You are about to DESTROY all infrastructure!       ║
╚══════════════════════════════════════════════════════╝
"@
        $confirmation = Read-Host "Are you sure? Type 'DESTROY' to confirm"
        if ($confirmation -eq "DESTROY") {
            $rgName = "rg-numbatwallet-$Environment-aue"
            Write-Warning "Deleting resource group: $rgName"
            az group delete --name $rgName --yes
            Write-Success "Infrastructure destroyed successfully"
        } else {
            Write-Warning "Destruction cancelled"
        }
        exit 0
    }

    if ($ValidateOnly) {
        Write-Info "Validating Bicep templates..."
        $result = az deployment sub validate `
            --location $Location `
            --template-file $MainBicep `
            --parameters $TempParams `
            --parameters postgresAdminUsername=$PostgresAdminUsername `
            --parameters postgresAdminPassword=$PlainPassword `
            --name $DeploymentName `
            2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ Validation successful!"
        } else {
            Write-Error "✗ Validation failed!"
            Write-Host $result
            exit 1
        }
    }
    elseif ($WhatIf) {
        Write-Info "Running what-if deployment..."
        az deployment sub what-if `
            --location $Location `
            --template-file $MainBicep `
            --parameters $TempParams `
            --parameters postgresAdminUsername=$PostgresAdminUsername `
            --parameters postgresAdminPassword=$PlainPassword `
            --name $DeploymentName
    }
    else {
        Write-Info "Starting deployment: $DeploymentName"
        Write-Info "Environment: $Environment"
        Write-Info "Location: $Location"
        Write-Host ""

        # Confirm deployment
        $confirm = Read-Host "Proceed with deployment? (y/n)"
        if ($confirm -ne 'y') {
            Write-Warning "Deployment cancelled"
            exit 0
        }

        # Run deployment
        Write-Info "Deploying infrastructure..."
        $result = az deployment sub create `
            --location $Location `
            --template-file $MainBicep `
            --parameters $TempParams `
            --parameters postgresAdminUsername=$PostgresAdminUsername `
            --parameters postgresAdminPassword=$PlainPassword `
            --name $DeploymentName `
            --output json | ConvertFrom-Json

        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ Deployment successful!"

            # Get outputs
            Write-Info "Retrieving deployment outputs..."
            $outputs = az deployment sub show `
                --name $DeploymentName `
                --query "properties.outputs" `
                --output json

            $outputs | Set-Content "$Environment-outputs.json"
            Write-Success "Outputs saved to: $Environment-outputs.json"

            # Store PostgreSQL password in Key Vault
            if ($PostgresAdminPassword) {
                $kvName = "kv-numbatwallet-$Environment-aue"
                Write-Info "Storing PostgreSQL password in Key Vault: $kvName"
                az keyvault secret set `
                    --vault-name $kvName `
                    --name "PostgresAdminPassword" `
                    --value $PlainPassword `
                    --output none
                Write-Success "✓ Password stored securely in Key Vault"
            }
        } else {
            Write-Error "✗ Deployment failed!"
            exit 1
        }
    }
}
finally {
    # Clean up
    if (Test-Path $TempParams) {
        Remove-Item $TempParams -Force
    }
    # Clear password from memory
    if ($PlainPassword -and $PlainPassword -ne "temp-password-for-validation") {
        Clear-Variable PlainPassword
    }
}

Write-Success @"
╔══════════════════════════════════════════════════════╗
║            Deployment Complete! 🎉                   ║
╚══════════════════════════════════════════════════════╝
"@