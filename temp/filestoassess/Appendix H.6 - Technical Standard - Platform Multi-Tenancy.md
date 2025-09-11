# Appendix H.6 - Technical Standard - Platform Multi-Tenancy

Technical Standard PM: Platform Multi-Tenancy

# PM-1: Partitioning into PKI and Identity Containers
**Requirement:**
Platform should be able to be partitioned into multiple PKI and Identity containers.
**Standards:** eIDAS 2.0, ISO/IEC 27001, ISO/IEC 27002

**Implementation by CredEntry:**
CredEntry supports multi-tenancy through logical partitioning into independent PKI and Identity containers. Each container provides isolated trust boundaries, ensuring data and cryptographic material separation. This enables secure multi-tenant deployments aligned with eIDAS 2.0 and ISO/IEC 27001/27002 standards.

# PM-2: Separate PKI Configuration
**Requirement:**
Platform containers should enable separate configuration of PKI.
**Standards:** eIDAS 2.0, ISO/IEC 27001, ISO/IEC 27002

**Implementation by CredEntry:**
CredEntry allows each tenant container to configure its own PKI, including certificate authorities, trust anchors, and cryptographic policies. This ensures independent key lifecycles and governance while maintaining compliance with eIDAS 2.0 and ISO/IEC standards.

# PM-3: Separate Identity Provider Configuration
**Requirement:**
Platform containers should enable separate configuration of Identity Providers.
**Standards:** eIDAS 2.0, ISO/IEC 27001, ISO/IEC 27002

**Implementation by CredEntry:**
CredEntry enables container-specific configuration of identity providers. Each tenant can integrate its own OIDC or SAML identity providers, supporting unique authentication systems while maintaining logical isolation across tenants.

# PM-4: Container-Specific Branding and Customisation
**Requirement:**
Platform containers should enable standalone branding and customisation (for integrated web interface).
**Standards:** OWASP ASVS

**Implementation by CredEntry:**
CredEntry provides customisation capabilities at the container level, allowing independent branding, theming, and interface adjustments for each tenant. This ensures that organisations can deliver a tailored user experience while maintaining compliance with OWASP ASVS requirements.

