# Appendix H.7 - Technical Standard - PRH - Repository and Hosting

# Technical Standard TS-x: Repository and Hosting
**Requirement**

**PRH-1:** Customer data must be stored within Commonwealth of Australia sovereign borders.
**Classification:** Must
**Reference Standard:** WA Government Offshoring Position

**Implementation by CredEntry**

CredEntry ensures that all customer data is stored and processed exclusively within the Commonwealth of Australia sovereign borders. The platform is deployed in **Microsoft Azure Australia East and Australia Central regions**, both certified for government and regulated workloads.

Data residency is enforced through the following measures:

- **Regional Restriction:** All databases, blob storage, and backup repositories are provisioned within Australian regions only.
- **Data Sovereignty Controls:** No replication, backup, or processing occurs outside Australia. Disaster recovery (DR) is also geo-restricted to Australia East and Australia Central.
- **Logical Segregation:** Customer environments are logically isolated to ensure tenant data integrity and compliance with WA Government Offshoring Position.
- **Compliance Alignment:** Azure Australia regions hold certifications including IRAP, ISO/IEC 27001, and ISO/IEC 27018, supporting both Australian Government and WA State Government security requirements.
An **architecture diagram** is provided below to illustrate data residency and hosting controls, showing:

- Azure Australia East/Central as the only active regions
- Network isolation (VNet, Azure Application Gateway, API Management)
- Data storage (Azure SQL, Blob Storage) restricted to Australian borders
- DR and backup replication within Australia only
