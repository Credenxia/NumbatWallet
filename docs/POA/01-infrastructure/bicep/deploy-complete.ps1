#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Complete deployment script for NumbatWallet POA infrastructure
    
.DESCRIPTION
    Orchestrates the deployment of all infrastructure components including:
    - Pre-flight checks
    - Azure Entra ID setup (generates script)
    - Infrastructure deployment (Bicep)
    - Post-deployment configuration
    - Validation
    
.PARAMETER Environment
    Target environment: dev, test, demo, or prod
    
.PARAMETER ResourceGroup
    Azure resource group name
    
.PARAMETER Location
    Azure region (default: australiaeast)
    
.PARAMETER SkipPreflightChecks
    Skip pre-flight validation checks
    
.PARAMETER SkipEntraSetup
    Skip Azure Entra ID setup script generation
    
.PARAMETER WhatIf
    Perform a what-if deployment without making changes
    
.EXAMPLE
    ./deploy-complete.ps1 -Environment dev -ResourceGroup rg-numbat-wallet-dev
    
.EXAMPLE
    ./deploy-complete.ps1 -Environment prod -ResourceGroup rg-numbat-wallet-prod -WhatIf
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'demo', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,
    
    [string]$Location = 'australiaeast',
    
    [switch]$SkipPreflightChecks,
    
    [switch]$SkipEntraSetup,
    
    [switch]$WhatIf
)

# Set strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Constants
$PROJECT_NAME = 'numbat-wallet'
$DEPLOYMENT_NAME = "deploy-$PROJECT_NAME-$Environment-$(Get-Date -Format 'yyyyMMddHHmmss')"
$BICEP_PATH = $PSScriptRoot
$MAIN_BICEP = Join-Path $BICEP_PATH 'main.bicep'
$PARAMETERS_FILE = Join-Path $BICEP_PATH "parameters.$Environment.json"

# Colors for output
$colors = @{
    Success = 'Green'
    Warning = 'Yellow'
    Error = 'Red'
    Info = 'Cyan'
    Header = 'Magenta'
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = 'White'
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "========================================" $colors.Header
    Write-ColorOutput $Title $colors.Header
    Write-ColorOutput "========================================" $colors.Header
    Write-Host ""
}

function Test-Prerequisites {
    Write-Header "Pre-flight Checks"
    
    $checks = @(
        @{
            Name = "Azure CLI"
            Command = { az version --output json | ConvertFrom-Json }
            MinVersion = "2.50.0"
        },
        @{
            Name = "Azure Bicep"
            Command = { az bicep version }
            MinVersion = "0.20.0"
        },
        @{
            Name = "PowerShell"
            Command = { $PSVersionTable.PSVersion }
            MinVersion = "7.0.0"
        }
    )
    
    $failed = $false
    
    foreach ($check in $checks) {
        Write-Host "Checking $($check.Name)... " -NoNewline
        try {
            $version = & $check.Command
            Write-ColorOutput "OK" $colors.Success
        }
        catch {
            Write-ColorOutput "FAILED" $colors.Error
            Write-ColorOutput "  Error: $_" $colors.Error
            $failed = $true
        }
    }
    
    # Check Azure subscription
    Write-Host "Checking Azure subscription... " -NoNewline
    try {
        $account = az account show --output json | ConvertFrom-Json
        Write-ColorOutput "OK" $colors.Success
        Write-ColorOutput "  Subscription: $($account.name) ($($account.id))" $colors.Info
    }
    catch {
        Write-ColorOutput "FAILED" $colors.Error
        Write-ColorOutput "  Please run 'az login' first" $colors.Error
        $failed = $true
    }
    
    # Check Bicep file exists
    Write-Host "Checking Bicep files... " -NoNewline
    if (Test-Path $MAIN_BICEP) {
        Write-ColorOutput "OK" $colors.Success
    }
    else {
        Write-ColorOutput "FAILED" $colors.Error
        Write-ColorOutput "  Main Bicep file not found: $MAIN_BICEP" $colors.Error
        $failed = $true
    }
    
    if ($failed) {
        throw "Pre-flight checks failed. Please resolve the issues above."
    }
    
    Write-ColorOutput "`nAll pre-flight checks passed!" $colors.Success
}

function New-ParametersFile {
    Write-Header "Creating Parameters File"
    
    if (Test-Path $PARAMETERS_FILE) {
        Write-ColorOutput "Parameters file already exists: $PARAMETERS_FILE" $colors.Warning
        $overwrite = Read-Host "Overwrite? (y/N)"
        if ($overwrite -ne 'y') {
            Write-ColorOutput "Using existing parameters file" $colors.Info
            return
        }
    }
    
    $parameters = @{
        '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters = @{
            environment = @{ value = $Environment }
            location = @{ value = $Location }
            adminEmail = @{ value = "admin@$PROJECT_NAME.wa.gov.au" }
            tags = @{
                value = @{
                    Environment = $Environment
                    Project = 'NumbatWallet'
                    ManagedBy = 'POA Team'
                    CreatedDate = (Get-Date -Format 'yyyy-MM-dd')
                    CostCenter = 'DPC2142'
                }
            }
        }
    }
    
    # Environment-specific overrides
    switch ($Environment) {
        'prod' {
            $parameters.parameters.dbSkuName = @{ value = 'Standard_D4ds_v5' }
            $parameters.parameters.dbStorageSizeGB = @{ value = 256 }
            $parameters.parameters.dbBackupRetentionDays = @{ value = 30 }
            $parameters.parameters.appServicePlanSku = @{ value = 'P2v3' }
        }
        'demo' {
            $parameters.parameters.dbSkuName = @{ value = 'Standard_D2ds_v5' }
            $parameters.parameters.dbStorageSizeGB = @{ value = 128 }
            $parameters.parameters.dbBackupRetentionDays = @{ value = 14 }
            $parameters.parameters.appServicePlanSku = @{ value = 'P1v3' }
        }
        default {
            # dev/test use defaults from Bicep
        }
    }
    
    $parameters | ConvertTo-Json -Depth 10 | Set-Content $PARAMETERS_FILE
    Write-ColorOutput "Created parameters file: $PARAMETERS_FILE" $colors.Success
}

function New-EntraIdSetupScript {
    Write-Header "Generating Azure Entra ID Setup Script"
    
    $scriptPath = Join-Path $PSScriptRoot "setup-entra-$Environment.ps1"
    
    # Deploy just the Entra ID module to get the setup script
    Write-ColorOutput "Generating Entra ID configuration script..." $colors.Info
    
    $entraOutput = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file (Join-Path $BICEP_PATH 'modules' 'entra-id.bicep') `
        --parameters environment=$Environment `
        --parameters adminPortalUrl="https://$PROJECT_NAME-admin-$Environment.azurewebsites.net" `
        --parameters apiUrl="https://$PROJECT_NAME-api-$Environment.azurewebsites.net" `
        --parameters keyVaultName="$PROJECT_NAME-kv-$Environment" `
        --query 'properties.outputs.setupScript.value' `
        --output tsv
    
    $entraOutput | Set-Content $scriptPath
    
    Write-ColorOutput "Azure Entra ID setup script generated: $scriptPath" $colors.Success
    Write-ColorOutput "`nIMPORTANT: Run the following command to set up Azure Entra ID:" $colors.Warning
    Write-ColorOutput "  PowerShell.exe -File $scriptPath" $colors.Warning
    Write-ColorOutput "`nThis script will:" $colors.Info
    Write-ColorOutput "  1. Create an App Registration" $colors.Info
    Write-ColorOutput "  2. Configure API permissions" $colors.Info
    Write-ColorOutput "  3. Create app roles (Admin, Issuer, Verifier, Operator)" $colors.Info
    Write-ColorOutput "  4. Generate client secret" $colors.Info
    Write-ColorOutput "  5. Output Key Vault commands to store secrets" $colors.Info
    
    $continue = Read-Host "`nHave you run the Entra ID setup script? (y/N)"
    if ($continue -ne 'y') {
        Write-ColorOutput "Please run the Entra ID setup script before continuing." $colors.Warning
        Write-ColorOutput "Deployment paused. Re-run this script after completing Entra ID setup." $colors.Warning
        exit 0
    }
}

function Deploy-Infrastructure {
    Write-Header "Deploying Infrastructure"
    
    # Validate the Bicep template
    Write-ColorOutput "Validating Bicep template..." $colors.Info
    $validation = az deployment group validate `
        --resource-group $ResourceGroup `
        --template-file $MAIN_BICEP `
        --parameters $PARAMETERS_FILE `
        --output json | ConvertFrom-Json
    
    if ($validation.error) {
        Write-ColorOutput "Validation failed:" $colors.Error
        Write-ColorOutput ($validation.error | ConvertTo-Json -Depth 10) $colors.Error
        throw "Bicep validation failed"
    }
    
    Write-ColorOutput "Validation passed!" $colors.Success
    
    # Perform deployment (or what-if)
    if ($WhatIf) {
        Write-ColorOutput "`nPerforming What-If analysis..." $colors.Info
        az deployment group what-if `
            --resource-group $ResourceGroup `
            --template-file $MAIN_BICEP `
            --parameters $PARAMETERS_FILE `
            --output table
    }
    else {
        Write-ColorOutput "`nDeploying infrastructure..." $colors.Info
        Write-ColorOutput "This may take 15-30 minutes..." $colors.Info
        
        $deployment = az deployment group create `
            --resource-group $ResourceGroup `
            --name $DEPLOYMENT_NAME `
            --template-file $MAIN_BICEP `
            --parameters $PARAMETERS_FILE `
            --output json | ConvertFrom-Json
        
        if ($deployment.properties.provisioningState -eq 'Succeeded') {
            Write-ColorOutput "`nDeployment succeeded!" $colors.Success
            
            # Display outputs
            Write-Header "Deployment Outputs"
            $outputs = $deployment.properties.outputs
            
            Write-ColorOutput "API URL: $($outputs.apiUrl.value)" $colors.Info
            Write-ColorOutput "Admin Portal URL: $($outputs.adminPortalUrl.value)" $colors.Info
            Write-ColorOutput "Key Vault: $($outputs.keyVaultName.value)" $colors.Info
            Write-ColorOutput "Application Insights: $($outputs.appInsightsName.value)" $colors.Info
            
            # Save outputs to file
            $outputsFile = Join-Path $PSScriptRoot "deployment-outputs-$Environment.json"
            $outputs | ConvertTo-Json -Depth 10 | Set-Content $outputsFile
            Write-ColorOutput "`nOutputs saved to: $outputsFile" $colors.Success
        }
        else {
            Write-ColorOutput "Deployment failed: $($deployment.properties.provisioningState)" $colors.Error
            throw "Deployment failed"
        }
    }
}

function Test-Deployment {
    Write-Header "Validating Deployment"
    
    Write-ColorOutput "Running post-deployment tests..." $colors.Info
    
    # Get deployment outputs
    $outputs = az deployment group show `
        --resource-group $ResourceGroup `
        --name $DEPLOYMENT_NAME `
        --query 'properties.outputs' `
        --output json | ConvertFrom-Json
    
    $tests = @(
        @{
            Name = "API Health Check"
            Url = "$($outputs.apiUrl.value)/health"
            ExpectedStatus = 200
        },
        @{
            Name = "Admin Portal"
            Url = $outputs.adminPortalUrl.value
            ExpectedStatus = 200
        },
        @{
            Name = "Application Insights"
            Command = {
                az monitor app-insights component show `
                    --resource-group $ResourceGroup `
                    --app $outputs.appInsightsName.value `
                    --output none
            }
        }
    )
    
    $failed = 0
    
    foreach ($test in $tests) {
        Write-Host "Testing $($test.Name)... " -NoNewline
        
        if ($test.Url) {
            try {
                $response = Invoke-WebRequest -Uri $test.Url -Method Head -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq $test.ExpectedStatus) {
                    Write-ColorOutput "PASSED" $colors.Success
                }
                else {
                    Write-ColorOutput "FAILED (Status: $($response.StatusCode))" $colors.Error
                    $failed++
                }
            }
            catch {
                Write-ColorOutput "FAILED" $colors.Error
                Write-ColorOutput "  Error: $_" $colors.Error
                $failed++
            }
        }
        elseif ($test.Command) {
            try {
                & $test.Command
                Write-ColorOutput "PASSED" $colors.Success
            }
            catch {
                Write-ColorOutput "FAILED" $colors.Error
                $failed++
            }
        }
    }
    
    if ($failed -gt 0) {
        Write-ColorOutput "`n$failed test(s) failed" $colors.Warning
        Write-ColorOutput "Note: Some services may take a few minutes to become available" $colors.Info
    }
    else {
        Write-ColorOutput "`nAll tests passed!" $colors.Success
    }
}

function Show-NextSteps {
    Write-Header "Next Steps"
    
    Write-ColorOutput "Deployment completed successfully!" $colors.Success
    Write-Host ""
    Write-ColorOutput "Next steps:" $colors.Info
    Write-ColorOutput "1. Configure Azure Entra ID users and role assignments" $colors.Info
    Write-ColorOutput "2. Set up API keys for DTP integration in Key Vault" $colors.Info
    Write-ColorOutput "3. Configure custom domain names (optional)" $colors.Info
    Write-ColorOutput "4. Set up Azure Front Door (production only)" $colors.Info
    Write-ColorOutput "5. Configure backup and disaster recovery" $colors.Info
    Write-ColorOutput "6. Review and configure alerts in Application Insights" $colors.Info
    Write-Host ""
    Write-ColorOutput "Useful commands:" $colors.Info
    Write-ColorOutput "  View logs: az webapp log tail --resource-group $ResourceGroup --name $PROJECT_NAME-api-$Environment" $colors.Info
    Write-ColorOutput "  Open portal: az webapp browse --resource-group $ResourceGroup --name $PROJECT_NAME-admin-$Environment" $colors.Info
    Write-ColorOutput "  View metrics: az monitor metrics list --resource $PROJECT_NAME-api-$Environment --resource-group $ResourceGroup --metric CpuPercentage --output table" $colors.Info
}

# Main execution
try {
    Write-Header "NumbatWallet POA Infrastructure Deployment"
    Write-ColorOutput "Environment: $Environment" $colors.Info
    Write-ColorOutput "Resource Group: $ResourceGroup" $colors.Info
    Write-ColorOutput "Location: $Location" $colors.Info
    Write-ColorOutput "What-If Mode: $WhatIf" $colors.Info
    
    # Create resource group if it doesn't exist
    Write-Host "`nChecking resource group... " -NoNewline
    $rgExists = az group exists --name $ResourceGroup --output tsv
    if ($rgExists -eq 'false') {
        Write-ColorOutput "Creating" $colors.Warning
        if (-not $WhatIf) {
            az group create --name $ResourceGroup --location $Location --output none
            Write-ColorOutput "Resource group created" $colors.Success
        }
    }
    else {
        Write-ColorOutput "Exists" $colors.Success
    }
    
    # Run pre-flight checks
    if (-not $SkipPreflightChecks) {
        Test-Prerequisites
    }
    
    # Create parameters file
    New-ParametersFile
    
    # Generate Entra ID setup script
    if (-not $SkipEntraSetup) {
        New-EntraIdSetupScript
    }
    
    # Deploy infrastructure
    Deploy-Infrastructure
    
    # Validate deployment
    if (-not $WhatIf) {
        Test-Deployment
        Show-NextSteps
    }
    
    Write-ColorOutput "`nDeployment complete!" $colors.Success
}
catch {
    Write-ColorOutput "`nDeployment failed!" $colors.Error
    Write-ColorOutput $_.Exception.Message $colors.Error
    Write-ColorOutput $_.ScriptStackTrace $colors.Error
    exit 1
}