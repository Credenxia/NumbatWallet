# Azure Pricing Calculator Input Guide
## Digital Wallet Infrastructure Configuration Parameters

**Document Version:** 2.0  
**Parent Document:** [Azure Justification and Pricing](./Azure_Justification_and_Pricing.md)  
**Last Updated:** December 2024

---

## Instructions for Azure Pricing Calculator

1. Navigate to: https://azure.microsoft.com/en-au/pricing/calculator/
2. Select **Currency**: Australian Dollar (AUD) in top-right corner
3. Select **Region**: Australia East (default for all services)
4. Add each service below with the specified configurations

---

## 1. PILOT PHASE Configuration
### Target: 10,000 users, 12-month pilot
### Total: ~$5,875 AUD/month (with support)

### 1.1 Azure Kubernetes Service (AKS)
- **Region**: Australia East
- **Tier**: Standard
- **Cluster Management**: 1 cluster
- **Operating System**: Linux
- **Category**: General purpose
- **Instance**: D4s v5 (4 vCPUs, 16 GB RAM)
- **Virtual Machines**: 2
- **Hours**: 730
- **Managed OS Disks**: 
  - Tier: Premium SSD
  - Disk size: P10 - 128 GB
  - Disks: 2
- **Monthly Cost**: $713.57

### 1.2 Azure Database for PostgreSQL Flexible Server
- **Region**: Australia East
- **Deployment option**: Flexible Server
- **Tier**: Burstable
- **Instance**: B4MS (4 vCore)
- **Hours**: 730
- **Storage**: 128 GB
- **Backup Storage**: 0 GB
- **High Availability**: Disabled
- **Redundancy**: LRS
- **Monthly Cost**: $990.88

### 1.3 Azure Key Vault
- **Region**: Australia East
- **Operations (Standard)**: 10,000 operations
- **Advanced Operations**: 1,000 operations
- **Certificate Renewals**: 50
- **HSM Protected Keys**: 100
- **Advanced HSM Protected Keys**: 100
- **Managed HSM Pool**: Standard B1 (if needed)
- **Monthly Cost**: $1,156.61

### 1.4 Azure Dedicated HSM
- **Note**: Not required for Pilot
- **Alternative**: Using Key Vault HSM-backed keys (included in Key Vault pricing)
- **Monthly Cost**: $0

### 1.5 Storage Account (Blob)
- **Region**: Australia East
- **Type**: Block Blob Storage
- **Performance**: Standard
- **Redundancy**: LRS
- **Capacity**: 500 GB
- **Write Operations**: 10 (×10,000) = 100K ops
- **Read Operations**: 50 (×10,000) = 500K ops
- **List/Create Operations**: 1 (×10,000) = 10K ops
- **All Other Operations**: 1 (×10,000) = 10K ops
- **Data Retrieval**: 10 GB
- **Monthly Cost**: ~$15

### 1.6 Azure Cache for Redis
- **Region**: Australia East
- **Tier**: Standard
- **Instance**: C2 (2.5 GB cache)
- **Instances**: 1
- **Hours**: 730
- **Monthly Cost**: $252.11

### 1.7 API Management
- **Region**: Australia East
- **Tier**: Standard
- **Base Units**: 1
- **Hours**: 730
- **Scale Out Units**: 0
- **Requests**: 10 (×10,000)
- **Self-hosted Gateway**: 0
- **Monthly Cost**: $1,079.24

### 1.8 Azure Front Door
- **Tier**: Azure Front Door (Classic)
- **Zone**: Australia
- **Outbound Data Transfer**: 500 GB
- **Inbound Data Transfer**: 100 GB
- **First 5 Rules**: 5
- **Additional Rules**: 0
- **Additional Domains**: 2
- **WAF Policy**: 1
- **Custom Rules**: 0
- **Monthly Cost**: $409.11

### 1.9 Virtual Network
- **Region**: Australia East
- **VNET 1 Region**: Australia East
- **VNET 2 Region**: Australia Central
- **Outbound Data Transfer**: 100 GB each
- **Monthly Cost**: $55.50

### 1.10 Azure Monitor
- **Region**: Australia East
- **Log Data Ingestion**: 100 GB
- **All other fields**: Leave at 0 or default
- **Monthly Cost**: $728.14

### 1.11 Azure Backup
- **Region**: Australia East
- **Type**: Azure VMs
- **Backup Policy Type**: Standard
- **VMs**: 2 (matches AKS nodes)
- **Average Protected Instance Size**: 50 GB
- **Retain daily backups for**: 30 days
- **Retain weekly backups for**: 8 weeks
- **Retain monthly backups for**: 6 months
- **Retain yearly backups for**: 10 years
- **Retain instant restore snapshots for**: 1 day
- **Average Daily Data Churn**: Low
- **Redundancy**: LRS
- **Monthly Cost**: $21.97

### 1.12 Azure NAT Gateway
- **Region**: Australia East
- **Tier**: Standard
- **Gateway Hours**: 730
- **Data Processed**: 100 GB
- **Monthly Cost**: $57.59

### 1.13 Azure Private Link
- **Region**: Australia East
- **Private Endpoints**: 5
- **Hours**: 730
- **Outbound Data Processed**: 50 GB
- **Inbound Data Processed**: 50 GB
- **Monthly Cost**: $57.82

### 1.14 Storage Account (Tables)
- **Region**: Australia East
- **Type**: Table Storage
- **Tier**: Standard
- **Redundancy**: LRS
- **Capacity**: 200 GB
- **Storage Transactions**: 100 (×10,000)
- **Monthly Cost**: $15.32

### 1.15 Azure Support
- **Tier**: Standard
- **Monthly Cost**: $154.18

### Development Tools (Not in Azure Calculator)
- **GitHub Copilot**: 5 seats × $45 = $225/month
- **Claude Code**: 3 seats × $200 = $600/month
- **Monthly Cost**: $825

---

## 2. SMALL PRODUCTION Configuration
### Target: 100,000 users, Annual contract
### Total: ~$11,562 AUD/month (with support)

### 2.1 Azure Kubernetes Service (AKS)
- **Region**: Australia East
- **Tier**: Standard
- **Cluster Management**: 1 cluster
- **Operating System**: Linux
- **Category**: General purpose
- **Instance**: D4s v5 (4 vCPUs, 16 GB RAM)
- **Virtual Machines**: 3
- **Hours**: 730
- **Managed OS Disks**:
  - Tier: Premium SSD
  - Disk size: P10 - 128 GB
  - Disks: 3
- **Monthly Cost**: $1,014.08

### 2.2 Azure Database for PostgreSQL Flexible Server
- **Region**: Australia East
- **Deployment option**: Flexible Server
- **Tier**: General Purpose
- **Instance**: D8ds v5 (8 vCore)
- **Hours**: 730
- **Storage**: 256 GB Premium SSD
- **Backup Storage**: 0 GB
- **High Availability**: Enabled
- **Redundancy**: LRS
- **Monthly Cost**: $2,305.91

### 2.3 Azure Key Vault
- **Region**: Australia East
- **Operations (Standard)**: 50,000 operations
- **Advanced Operations**: 5,000 operations
- **Certificate Renewals**: 100
- **HSM Protected Keys**: 500
- **Managed HSM Pool**: Standard B1
- **Monthly Cost**: $4,835.49

### 2.4 Managed HSM
- **Note**: Included in Key Vault pricing above
- **Alternative**: Could use Key Vault HSM-backed keys to save ~$3,500/month
- **Monthly Cost**: Included in Key Vault

### 2.5 Storage Account (Blob)
- **Region**: Australia East
- **Type**: Block Blob Storage
- **Performance**: Standard
- **Redundancy**: LRS
- **Capacity**: 1,000 GB/1TB
- **Write Operations**: 50 (×10,000) = 500K ops
- **Read Operations**: 200 (×10,000) = 2M ops
- **List/Create Operations**: 5 (×10,000) = 50K ops
- **All Other Operations**: 5 (×10,000) = 50K ops
- **Data Retrieval**: 50 GB
- **Monthly Cost**: $36.89

### 2.6 Azure Cache for Redis
- **Region**: Australia East
- **Tier**: Standard
- **Instance**: C3 (6 GB cache)
- **Instances**: 1
- **Hours**: 730
- **Monthly Cost**: $506.48

### 2.7 API Management
- **Region**: Australia East
- **Tier**: Standard
- **Base Units**: 1
- **Hours**: 730
- **Scale Out Units**: 0
- **Requests**: 10 (×10,000)
- **Self-hosted Gateway**: 0
- **Note**: 1 unit handles up to 3,000 req/sec
- **Monthly Cost**: $1,079.24

### 2.8 Azure Front Door
- **Tier**: Azure Front Door (Classic)
- **Zone**: Australia
- **Outbound Data Transfer**: 1,000 GB/1TB
- **Inbound Data Transfer**: 100 GB
- **First 5 Rules**: 5
- **Additional Rules**: 0
- **Additional Domains**: 5
- **WAF Policy**: 1
- **Custom Rules**: 5
- **Monthly Cost**: $655.80

### 2.9 Virtual Network
- **Region**: Australia East
- **VNET 1 Region**: Australia East
- **VNET 2 Region**: Australia Central
- **Outbound Data Transfer**: 100 GB each
- **Monthly Cost**: $55.50

### 2.10 Azure Monitor
- **Region**: Australia East
- **Auxiliary Logs**:
  - Data ingestion (no processing): 50 GB, 30 days retention
  - Data ingestion (with processing): 50 GB, 30 days retention
- **Basic/Analytics Logs**: Leave at 0
- **Interactive Retention**: 3 days (default)
- **Alert Rules**: 0
- **Standard Web Test**: 0
- **All other fields**: Leave at 0
- **Monthly Cost**: $755.20

### 2.11 Azure Backup
- **Region**: Australia East
- **Type**: Azure VMs
- **Backup Policy Type**: Standard
- **VMs**: 3 (matches AKS nodes)
- **Average Protected Instance Size**: 50 GB
- **Retain daily backups for**: 30 days
- **Retain weekly backups for**: 8 weeks
- **Retain monthly backups for**: 6 months
- **Retain yearly backups for**: 10 years
- **Retain instant restore snapshots for**: 1 day
- **Average Daily Data Churn**: Low
- **Redundancy**: LRS
- **Monthly Cost**: $32.95

### 2.12 Azure NAT Gateway
- **Region**: Australia East
- **Tier**: Standard
- **Gateway Hours**: 730
- **Data Processed**: 100 GB
- **Monthly Cost**: $57.59

### 2.13 Azure Private Link
- **Region**: Australia East
- **Private Endpoints**: 5
- **Hours**: 730
- **Outbound Data Processed**: 50 GB
- **Inbound Data Processed**: 50 GB
- **Monthly Cost**: $57.82

### 2.14 Storage Account (Tables)
- **Region**: Australia East
- **Type**: Table Storage
- **Tier**: Standard
- **Redundancy**: LRS
- **Capacity**: 200 GB
- **Storage Transactions**: 100 (×10,000)
- **Monthly Cost**: $15.32

### 2.15 Azure Support
- **Tier**: Standard
- **Monthly Cost**: $154.18

### Development Tools (Not in Azure Calculator)
- **GitHub Copilot**: 10 seats × $45 = $450/month
- **Claude Code**: 5 seats × $200 = $1,000/month
- **Monthly Cost**: $1,450

---

## 3. MEDIUM PRODUCTION Configuration
### Target: 500,000 users, Annual contract
### Total: ~$19,558 AUD/month (with support)

### 3.1 Azure Kubernetes Service (AKS)
- **Region**: Australia East
- **Tier**: Standard
- **Cluster Management**: 2 clusters
- **Operating System**: Linux
- **Category**: General purpose
- **Instance**: D4s v5 (4 vCPUs, 16 GB RAM)
- **Virtual Machines**: 6
- **Hours**: 730
- **Managed OS Disks**:
  - Tier: Premium SSD
  - Disk size: P10 - 128 GB
  - Disks: 6
- **Availability Zones**: Multi-AZ deployment
- **Monthly Cost**: $1,936.99

### 3.2 Azure Database for PostgreSQL Flexible Server
- **Region**: Australia East
- **Deployment option**: Flexible Server
- **Tier**: General Purpose
- **Instance**: D8ds v5 (8 vCore)
- **Hours**: 730
- **Storage**: 512 GB Premium SSD
- **Backup Storage**: 512 GB
- **High Availability**: Enabled
- **Redundancy**: LRS
- **Monthly Cost**: $4,686.82

### 3.3 Azure Key Vault
- **Region**: Australia East
- **Operations (Standard)**: 100,000 operations
- **Advanced Operations**: 10,000 operations
- **Certificate Renewals**: 200
- **HSM Protected Keys**: 1,000
- **Managed HSM Pool**: Standard B1
- **Monthly Cost**: $6,069.15

### 3.4 Azure Dedicated HSM
- **Note**: Not required for Medium Production
- **Alternative**: Using Key Vault HSM-backed keys (included in Key Vault pricing)
- **Monthly Cost**: $0

### 3.5 Storage Account (Blob)
- **Region**: Australia East
- **Type**: Block Blob Storage
- **Performance**: Standard
- **Redundancy**: GRS
- **Capacity**: 5,000 GB/5TB
- **Write Operations**: 250 (×10,000) = 2.5M ops
- **Read Operations**: 1,000 (×10,000) = 10M ops
- **List/Create Operations**: 25 (×10,000) = 250K ops
- **All Other Operations**: 50 (×10,000) = 500K ops
- **Data Retrieval**: 250 GB
- **Geo-replication Data Transfer**: 1TB
- **Monthly Cost**: $513.21

### 3.6 Azure Cache for Redis
- **Region**: Australia East
- **Tier**: Premium
- **Instance**: P1 (6 GB cache)
- **Instances**: 1
- **Hours**: 730
- **Monthly Cost**: $623.53

### 3.7 API Management
- **Region**: Australia East
- **Tier**: Standard
- **Base Units**: 2
- **Hours**: 730
- **Scale Out Units**: 0
- **Requests**: 10 (×10,000)
- **Self-hosted Gateway**: 0
- **Note**: 2 units handle up to 6,000 req/sec
- **Monthly Cost**: $2,158.49

### 3.8 Azure Front Door
- **Tier**: Azure Front Door Premium
- **Zone**: Australia
- **Base Cost**: 1 instance
- **Outbound Data Transfer to Client**: 5TB
- **Outbound Data Transfer to Origin**: 1TB
- **Requests**: 0 (included in base)
- **Monthly Cost**: $1,519.21

### 3.9 Virtual Network
- **Region**: Australia East + Australia Central
- **VNET 1 Region**: Australia East
- **VNET 2 Region**: Australia Central
- **Outbound Data Transfer**: 
  - VNET 1: 500 GB
  - VNET 2: 100 GB
- **Note**: ExpressRoute not needed - cloud-native architecture
- **Monthly Cost**: $166.51

### 3.10 Azure Monitor
- **Region**: Australia East
- **Auxiliary Logs**:
  - Data ingestion (no processing): 100 GB, 30 days retention
  - Data ingestion (with processing): 100 GB, 30 days retention
- **Metric Signals Monitored**: 47
- **Log Signals Monitored**: 1
- **Interactive Retention**: 3 days
- **Long-term Retention**: 6,000 GB
- **Monthly Cost**: $1,325.47

### 3.11 Azure Backup
- **Region**: Australia East
- **Type**: Azure VMs
- **Backup Policy Type**: Standard
- **VMs**: 6 (matches AKS nodes)
- **Average Protected Instance Size**: 50 GB
- **Retain daily backups for**: 30 days
- **Retain weekly backups for**: 8 weeks
- **Retain monthly backups for**: 6 months
- **Retain yearly backups for**: 10 years
- **Retain instant restore snapshots for**: 2 days
- **Average Daily Data Churn**: Low
- **Redundancy**: LRS
- **Monthly Cost**: $66.51

### 3.12 Azure NAT Gateway
- **Region**: Australia East
- **Tier**: Standard
- **Gateway Hours**: 730
- **Data Processed**: 500 GB
- **Gateways**: 1 (redundant within region)
- **Monthly Cost**: $85.34

### 3.13 Azure Private Link
- **Region**: Australia East
- **Private Endpoints**: 15
- **Hours**: 730
- **Outbound Data Processed**: 250 GB
- **Inbound Data Processed**: 250 GB
- **Monthly Cost**: $176.53

### 3.14 Storage Account (Tables)
- **Region**: Australia East
- **Type**: Table Storage
- **Tier**: Standard
- **Redundancy**: LRS
- **Capacity**: 1,000 GB/1TB
- **Storage Transactions**: 500 (×10,000)
- **Monthly Cost**: $76.60

### 3.15 Azure Support
- **Tier**: Standard
- **Monthly Cost**: $154.18

### Development Tools (Not in Azure Calculator)
- **GitHub Copilot**: 15 seats × $45 = $675/month
- **Claude Code**: 8 seats × $200 = $1,600/month
- **Monthly Cost**: $2,275

---

## 4. LARGE PRODUCTION Configuration
### Target: 2,000,000+ users, Annual contract
### Total: $56,794.09 AUD/month (with support)

### 4.1 Azure Kubernetes Service (AKS)
- **Region**: Australia East
- **Tier**: Standard
- **Cluster Management**: 2 clusters
- **Operating System**: Linux
- **Category**: General purpose
- **Instance**: D8s v5 (8 vCPUs, 32 GB RAM)
- **Virtual Machines**: 12
- **Hours**: 730
- **Managed OS Disks**:
  - Tier: Premium SSD
  - Disk size: P15 - 256 GB
  - Disks: 12
- **Availability Zones**: Multi-AZ
- **Auto-scaling**: Enabled (12 min, 20 max)
- **Monthly Cost**: $6,715.53

### 4.2 Azure Database for PostgreSQL Flexible Server
- **Region**: Australia East
- **Deployment option**: Flexible Server
- **Tier**: General Purpose
- **Instance**: D32ds v5 (32 vCore)
- **Hours**: 730
- **Storage**: 2,048 GB Premium SSD
- **Backup Storage**: 2,048 GB
- **High Availability**: Enabled
- **Redundancy**: GRS
- **Read Replicas**: 1 replica quoted (not in calculator)
- **Monthly Cost (Primary)**: $19,047.25
- **Monthly Cost (With Replica)**: ~$28,000 (add ~$9K for replica when needed)

### 4.3 Azure Key Vault
- **Region**: Australia East
- **Operations (Standard)**: 500,000 operations
- **Advanced Operations**: 50,000 operations
- **Certificate Renewals**: 500
- **HSM Protected Keys**: 5,000
- **Managed HSM Pool**: Standard B1
- **Monthly Cost**: $13,626.66

### 4.4 Azure Dedicated HSM (Removed)
- **Note**: Replaced with Key Vault Managed HSM Pool (included in Key Vault pricing)
- **Rationale**: Managed HSM provides Level 3 security at lower cost
- **Monthly Cost**: $0

### 4.5 Storage Account (Blob)
- **Region**: Australia East
- **Type**: Block Blob Storage
- **Performance**: Standard
- **Redundancy**: RA-GRS
- **Capacity**: 10,000 GB/10TB
- **Write Operations**: 1,000 (×10,000) = 10M ops
- **Read Operations**: 5,000 (×10,000) = 50M ops
- **List/Create Operations**: 100 (×10,000) = 1M ops
- **All Other Operations**: 100 (×10,000) = 1M ops
- **Data Retrieval**: 1,000 GB
- **Geo-replication Data Transfer**: 1TB
- **Monthly Cost**: $1,143.14

### 4.6 Azure Cache for Redis
- **Region**: Australia East
- **Tier**: Premium
- **Instance**: P2 (13 GB cache)
- **Instances**: 1 with 2 replicas
- **Hours**: 730
- **Monthly Cost**: $2,498.61

### 4.7 API Management
- **Region**: Australia East
- **Tier**: Premium
- **Base Units**: 1
- **Hours**: 730
- **Scale Out Units**: 0
- **Requests**: Unlimited (included in Premium)
- **Self-hosted Gateway**: 0
- **VNET Integration**: Enabled
- **Note**: 1 Premium unit = 10 Standard units capacity
- **Monthly Cost**: $4,316.99

### 4.8 Azure Front Door
- **Tier**: Azure Front Door Premium
- **Zone**: Australia
- **Base Cost**: 1 instance
- **Outbound Data Transfer to Client**: 20TB
- **Outbound Data Transfer to Origin**: 5TB
- **Requests**: 0 (included in base)
- **Monthly Cost**: $4,385.94

### 4.9 Virtual Network
- **Region**: Australia East + Australia Central
- **VNET 1 Region**: Australia East
- **VNET 2 Region**: Australia Central
- **Outbound Data Transfer**: 500 GB each
- **VPN Gateway**: Optional - S2S for agency connections if needed ($200/month)
- **Network Watcher**: Enabled
- **Note**: ExpressRoute not required - fully cloud-native, remote-first architecture
- **Monthly Cost**: ~$280

### 4.10 Azure Monitor
- **Region**: Australia East
- **Auxiliary Logs**:
  - Data ingestion (no processing): 200 GB, 30 days retention
  - Data ingestion (with processing): 197 GB, 30 days retention
- **Basic Logs**: 0
- **Analytics Logs**: 0
- **Interactive Retention**: 3 days
- **Long-term Retention**: 0 (too expensive)
- **Application Insights**: Included
- **Alert Rules**: 7
- **Monthly Cost**: $3,559.74

### 4.11 Azure Backup
- **Region**: Australia East
- **Type**: Azure VMs
- **Backup Policy Type**: Standard
- **VMs**: 20
- **Average Protected Instance Size**: 50 GB
- **Retain daily backups for**: 30 days
- **Retain weekly backups for**: 8 weeks
- **Retain monthly backups for**: 60 months
- **Retain yearly backups for**: 10 years
- **Retain instant restore snapshots for**: 5 days
- **Average Daily Data Churn**: Low
- **Redundancy**: LRS
- **Monthly Cost**: $236.41

### 4.12 Azure NAT Gateway
- **Region**: Australia East
- **Tier**: Standard
- **Gateway Hours**: 730
- **Data Processed**: 500 GB
- **Gateways**: 1
- **Monthly Cost**: $85.34

### 4.13 Azure Private Link
- **Region**: Australia East
- **Private Endpoints**: 50
- **Hours**: 730
- **Outbound Data Processed**: 1,000 GB
- **Inbound Data Processed**: 1,000 GB
- **Monthly Cost**: $593.59

### 4.14 Storage Account (Tables)
- **Region**: Australia East
- **Type**: Table Storage
- **Tier**: Standard
- **Redundancy**: LRS
- **Capacity**: 2,000 GB/2TB
- **Storage Transactions**: 1,000 (×10,000)
- **Monthly Cost**: $153.19

### 4.15 Azure Support
- **Tier**: Standard
- **Monthly Cost**: $154.18

### Development Tools (Not in Azure Calculator)
- **GitHub Copilot**: 25 seats × $45 = $1,125/month
- **Claude Code**: 12 seats × $200 = $2,400/month
- **Azure DevOps**: Enterprise plan
- **Monthly Cost**: $3,525+

---

## Cost Summary Comparison

| Service | Pilot | Small | Medium | Large |
| --- | --- | --- | --- | --- |
| **Target Users** | 10,000 | 100,000 | 500,000 | 2,000,000+ |
| **AKS Nodes** | 2 | 3 | 6 | 12 |
| **PostgreSQL** | D4ds v5 | D8ds v5 | D16s v3 | D32s v3 |
| **Storage** | 500GB | 1TB | 5TB | 20TB |
| **Redis Cache** | Standard C2 | Premium P1 | Premium P3 | Premium P5 |
| **API Management** | Standard 1 unit | Standard 2 units | Standard 3 units | Premium 2 units |
| **Backup Storage** | 1TB LRS | 1TB LRS | 5TB GRS | 20TB RA-GRS |
| **Regions** | 1 | 1 | 2 | 4+ |
| **Infrastructure Total** | $11,654 | $15,723 | ~$50,000 | ~$100,000 |

---

## Scaling Guidelines

### From Pilot to Small Production:
- Add 1 AKS node
- Upgrade PostgreSQL from D4 to D8
- Double storage capacity
- Upgrade Redis to Premium tier
- Add API Management unit

### From Small to Medium Production:
- Double AKS nodes and upgrade to D8s
- Upgrade PostgreSQL to D16s with read replicas
- 5× storage capacity with GRS
- Upgrade to clustered Redis
- Move to Premium API Management
- Add multi-region support

### From Medium to Large Production:
- Double nodes again, upgrade to D16s
- Move to Business Critical PostgreSQL
- 4× storage with RA-GRS
- Geo-replicated Redis
- Full multi-region deployment
- Add ExpressRoute and advanced networking
- Implement continuous backup

---

## Currency Conversion Note

The Azure Calculator may default to USD. To view prices in AUD:
1. Look for the currency selector in the top-right corner
2. Select "Australia - Dollar ($) AUD"
3. All prices will automatically convert to Australian Dollars

Exchange rate used: 1 USD = 1.54 AUD (approximate)

---

[Back to Azure Justification](./Azure_Justification_and_Pricing.md) | [Back to Master PRD](./PRD_Master.md)