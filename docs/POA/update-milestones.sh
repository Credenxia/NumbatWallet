#!/bin/bash

echo "Updating milestones to chronological order..."

# Update milestone dates for proper chronological order
# Pre-dev milestones should be before official POA starts

# Update 000-PreDev-WalletApp to Sept 20 (was Sept 12 - too early)
gh api repos/Credenxia/NumbatWallet/milestones/12 --method PATCH \
  -f due_on="2025-09-20T23:59:59Z" \
  -f description="Complete wallet application development including UI/UX, architecture, and core features"

# Update 001-Week0-Foundation to Sept 27 (preparation week)
gh api repos/Credenxia/NumbatWallet/milestones/1 --method PATCH \
  -f due_on="2025-09-27T23:59:59Z" \
  -f title="001-Week0-Foundation-Setup" \
  -f description="Azure infrastructure setup, database configuration, and development environment preparation"

# Update 000-PreDev-Standards to Sept 24
gh api repos/Credenxia/NumbatWallet/milestones/13 --method PATCH \
  -f due_on="2025-09-24T23:59:59Z" \
  -f description="Complete standards compliance implementation (ISO 18013-5/7, W3C VC/DID, OpenID4VCI/VP, TDIF)"

# Update 000-PreDev-PKI to Sept 26
gh api repos/Credenxia/NumbatWallet/milestones/14 --method PATCH \
  -f due_on="2025-09-26T23:59:59Z" \
  -f description="Complete PKI infrastructure with IACA, Document Signing Certificates, trust lists, and HSM integration"

# Keep 000-PreDev-Integration at Sept 30
gh api repos/Credenxia/NumbatWallet/milestones/15 --method PATCH \
  -f description="Complete all integrations including ServiceWA SDK, WA IdX, and backend APIs"

# Update official POA week milestones
# 002-Week1-Infrastructure - Oct 4 (Official Week 1)
gh api repos/Credenxia/NumbatWallet/milestones/10 --method PATCH \
  -f due_on="2025-10-04T23:59:59Z" \
  -f title="002-Week1-POA-Deployment" \
  -f description="Official POA Week 1: Deploy to Azure, deliver SDKs, ServiceWA integration workshop"

# 003-Week2-Backend - Oct 11 (Official Week 2)
gh api repos/Credenxia/NumbatWallet/milestones/6 --method PATCH \
  -f due_on="2025-10-11T23:59:59Z" \
  -f title="003-Week2-POA-Features" \
  -f description="Official POA Week 2: Complete all features, authentication, credential operations"

# 005-Week4-Demo - Oct 18 (Official Week 3 - Demo Week)
gh api repos/Credenxia/NumbatWallet/milestones/3 --method PATCH \
  -f due_on="2025-10-18T23:59:59Z" \
  -f title="004-Week3-POA-Demo" \
  -f description="Official POA Week 3: Live demonstration to DGov evaluation panel"

# 006-Week5-UAT - Oct 25 (Testing Support)
gh api repos/Credenxia/NumbatWallet/milestones/4 --method PATCH \
  -f due_on="2025-10-25T23:59:59Z" \
  -f title="005-Week4-POA-Testing" \
  -f description="Support DGov UAT testing, issue resolution, performance optimization"

# 007-Week6-Handover - Nov 1 (Final Week)
gh api repos/Credenxia/NumbatWallet/milestones/5 --method PATCH \
  -f due_on="2025-11-01T23:59:59Z" \
  -f title="006-Week5-POA-Evaluation" \
  -f description="Final evaluation, knowledge transfer, and contract decision"

echo "Milestones updated successfully!"

# Now assign issues to milestones
echo "Assigning issues to appropriate milestones..."

# Assign wallet issues to PreDev-WalletApp milestone
for issue in 62 63 64 65 66 67 68 69 70 71; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "000-PreDev-WalletApp" 2>/dev/null || true
done

# Assign standards issues to PreDev-Standards milestone
for issue in 72 73 74 75 76 77 78 79 80 81 82 83 84 85 86; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "000-PreDev-Standards" 2>/dev/null || true
done

# Assign PKI issues to PreDev-PKI milestone (already done)
# Issues 87-90 already assigned

# Assign admin issues to Week3-Demo milestone
for issue in 91 92 93; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "004-Week3-POA-Demo" 2>/dev/null || true
done

# Assign feature issues appropriately
gh issue edit 94 --repo Credenxia/NumbatWallet --milestone "003-Week2-POA-Features" 2>/dev/null || true  # Offline verification
gh issue edit 95 --repo Credenxia/NumbatWallet --milestone "003-Week2-POA-Features" 2>/dev/null || true  # QR codes
gh issue edit 96 --repo Credenxia/NumbatWallet --milestone "004-Week3-POA-Demo" 2>/dev/null || true     # NFC
gh issue edit 97 --repo Credenxia/NumbatWallet --milestone "003-Week2-POA-Features" 2>/dev/null || true  # Revocation
gh issue edit 98 --repo Credenxia/NumbatWallet --milestone "003-Week2-POA-Features" 2>/dev/null || true  # Data minimization
gh issue edit 99 --repo Credenxia/NumbatWallet --milestone "003-Week2-POA-Features" 2>/dev/null || true  # Transaction history

echo "Issues assigned to milestones!"