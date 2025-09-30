// Development Environment Parameters
using '../main.bicep'

param environment = 'dev'
param location = 'australiaeast'
param baseName = 'numbatwallet'
param adminEmail = 'admin@numbatwallet.gov.au'

param tags = {
  Environment: 'dev'
  Project: 'NumbatWallet'
  Purpose: 'Digital Identity Wallet - Development'
  Compliance: 'TDIF'
  DataClassification: 'Sensitive'
  ManagedBy: 'Bicep'
  CostCenter: 'Development'
  Owner: 'Platform Team'
  CreatedDate: '2025-09-23'
}