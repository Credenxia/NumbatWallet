#!/bin/bash

echo "=== REORGANIZING BACKEND ISSUES TO PROPER MILESTONES ==="
echo "Starting at $(date)"
echo ""

# Move backend development issues from Infrastructure to Standards milestone
echo "Moving backend issues to 001-PreDev-Standards milestone..."

# Issue #10: Backend project structure - belongs in Standards/Backend setup
gh issue edit 10 --repo Credenxia/NumbatWallet --milestone "001-PreDev-Standards"
echo "✅ Moved #10 (Backend project structure) to Standards"

# Issue #11: Health check endpoints - belongs in Standards/Backend setup
gh issue edit 11 --repo Credenxia/NumbatWallet --milestone "001-PreDev-Standards"
echo "✅ Moved #11 (Health check endpoints) to Standards"

# Issue #31: Database schema design - belongs in Standards/Backend setup
gh issue edit 31 --repo Credenxia/NumbatWallet --milestone "001-PreDev-Standards"
echo "✅ Moved #31 (Database schema design) to Standards"

# Issue #33: Swagger/OpenAPI documentation - belongs in Standards/Backend setup
gh issue edit 33 --repo Credenxia/NumbatWallet --milestone "001-PreDev-Standards"
echo "✅ Moved #33 (Swagger/OpenAPI documentation) to Standards"

echo ""
echo "Moving SDK issues to Features milestone..."

# Issue #12: Flutter SDK - belongs in Features/SDK development
gh issue edit 12 --repo Credenxia/NumbatWallet --milestone "020-Week2-POA-Features"
echo "✅ Moved #12 (Flutter SDK initialization) to Features"

# Issue #39: .NET SDK - belongs in Features/SDK development
gh issue edit 39 --repo Credenxia/NumbatWallet --milestone "020-Week2-POA-Features"
echo "✅ Moved #39 (.NET SDK project setup) to Features"

echo ""
echo "=== REORGANIZATION COMPLETE ==="
echo ""
echo "Infrastructure milestone now contains only infrastructure issues:"
echo "- Azure setup (#1)"
echo "- PostgreSQL (#2)"
echo "- Container Registry (#3)"
echo "- Key Vault (#4)"
echo "- Virtual Network (#26)"
echo "- Application Gateway (#27)"
echo "- App Service Plan (#28)"
echo ""
echo "Backend issues moved to Standards milestone:"
echo "- Backend project structure (#10)"
echo "- Health check endpoints (#11)"
echo "- Database schema design (#31)"
echo "- Swagger documentation (#33)"
echo ""
echo "SDK issues moved to Features milestone:"
echo "- Flutter SDK (#12)"
echo "- .NET SDK (#39)"
echo ""
echo "Completed at $(date)"