#!/bin/bash

echo "=== SETTING UP PROJECT BOARD WITH CORRECT CHRONOLOGICAL ORDER ==="
echo "Project: NumbatWallet POA Phase (#18)"
echo "Starting at $(date)"
echo ""

# Project and Field IDs
PROJECT_ID="PVT_kwDOBBJaks4BCwXX"
START_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04RmU"
TARGET_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04Sug"
RESOURCE_FIELD="PVTSSF_lADOBBJaks4BCwXXzg05B2s"
STATUS_FIELD="PVTSSF_lADOBBJaks4BCwXXzg038zo"

# Resource option IDs
DEV1_BACKEND="af6cd89f"
DEV2_INFRA="51a08165"
BOTH="db524408"
UNASSIGNED="b505bd7a"

# Function to add issue to project and set fields
add_to_project() {
    local issue_num=$1
    local start_date=$2
    local target_date=$3
    local resource=$4
    local description=$5

    echo "  #$issue_num - $description"

    # Get issue node ID
    NODE_ID=$(gh api repos/Credenxia/NumbatWallet/issues/$issue_num --jq .node_id 2>/dev/null)

    if [ -z "$NODE_ID" ]; then
        echo "    ⚠️ Issue not found"
        return
    fi

    # Add to project
    ITEM_ID=$(gh api graphql -f query="
    mutation {
        addProjectV2ItemById(input: {
            projectId: \"$PROJECT_ID\"
            contentId: \"$NODE_ID\"
        }) {
            item { id }
        }
    }" --jq '.data.addProjectV2ItemById.item.id' 2>/dev/null)

    if [ -z "$ITEM_ID" ]; then
        # Get existing item ID if already in project
        ITEM_ID=$(gh api graphql -f query="
        {
            repository(owner: \"Credenxia\", name: \"NumbatWallet\") {
                issue(number: $issue_num) {
                    projectItems(first: 10) {
                        nodes {
                            id
                            project { id }
                        }
                    }
                }
            }
        }" --jq ".data.repository.issue.projectItems.nodes[] | select(.project.id == \"$PROJECT_ID\") | .id" 2>/dev/null)
    fi

    if [ ! -z "$ITEM_ID" ]; then
        # Set all fields in one go for efficiency
        gh api graphql -f query="
        mutation {
            startDate: updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$START_DATE_FIELD\"
                value: { date: \"$start_date\" }
            }) { projectV2Item { id } }

            targetDate: updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$TARGET_DATE_FIELD\"
                value: { date: \"$target_date\" }
            }) { projectV2Item { id } }

            resource: updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$RESOURCE_FIELD\"
                value: { singleSelectOptionId: \"$resource\" }
            }) { projectV2Item { id } }
        }" > /dev/null 2>&1

        echo "    ✅ Added with dates: $start_date to $target_date"
    else
        echo "    ⚠️ Could not add to project"
    fi
}

echo "PHASE 1: PRE-DEVELOPMENT (Sept 16-30)"
echo "======================================="

echo ""
echo "1.1 Wallet Application (Sept 16-19) - Mobile Team"
echo "---------------------------------------------------"
# These are the actual wallet issues we created
add_to_project 72 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-100: Wallet architecture"
add_to_project 73 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-101: UI/UX design"
# Additional wallet issues if they exist
for i in 74 75 76 77; do
    add_to_project $i "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "Wallet feature"
done

echo ""
echo "1.2 Standards Implementation (Sept 16-23) - Standards Team"
echo "-----------------------------------------------------------"
add_to_project 62 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "JWT-VC and JSON-LD"
add_to_project 63 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "Credential manifest"
# Add any other standards issues here
for i in 110 111 112 113 114 115 116 117 118 119 120; do
    add_to_project $i "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "Standards compliance" 2>/dev/null
done

echo ""
echo "1.3 PKI Infrastructure (Sept 20-25) - Security Team"
echo "----------------------------------------------------"
add_to_project 64 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-125: IACA root certificates"
add_to_project 65 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-126: Document signing certificates"
add_to_project 66 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-127: Trust list management"
add_to_project 67 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-128: HSM integration"
add_to_project 68 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-129: Certificate lifecycle"
add_to_project 69 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-130: Revocation registry"
add_to_project 70 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-131: Key rotation"

echo ""
echo "1.4 Infrastructure Foundation (Sept 23-27) - DevOps Team"
echo "---------------------------------------------------------"
add_to_project 1 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-001: Azure subscription"
add_to_project 2 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-002: PostgreSQL setup"
add_to_project 3 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-003: Container registry"
add_to_project 4 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-005: Key Vault"
add_to_project 26 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-004: Virtual network"
add_to_project 28 "2025-09-23" "2025-09-27" "$DEV2_INFRA" "POA-007: App Service Plan"

echo ""
echo "1.5 Backend Foundation (Sept 23-27) - Backend Team"
echo "---------------------------------------------------"
add_to_project 10 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-012: Backend structure"
add_to_project 11 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-013: Health checks"
add_to_project 31 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-010: Database schema"
add_to_project 33 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-014: Swagger/OpenAPI"

echo ""
echo "1.6 SDK Development (Sept 23-27) - SDK Team"
echo "--------------------------------------------"
add_to_project 12 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-015: Flutter SDK init"
add_to_project 39 "2025-09-23" "2025-09-27" "$DEV1_BACKEND" "POA-033: .NET SDK setup"
add_to_project 41 "2025-09-23" "2025-09-30" "$DEV1_BACKEND" "POA-035: TypeScript SDK"

echo ""
echo "1.7 Integration Preparation (Sept 27-30) - Integration Team"
echo "------------------------------------------------------------"
# Add integration-specific issues here
add_to_project 14 "2025-09-27" "2025-09-30" "$BOTH" "POA-022: WA IdX integration"
add_to_project 18 "2025-09-27" "2025-09-30" "$BOTH" "POA-031: Flutter auth module"

echo ""
echo "PHASE 2: OFFICIAL POA (Oct 1-18)"
echo "================================="

echo ""
echo "2.1 Week 1 - Deployment (Oct 1-4) - All Teams"
echo "----------------------------------------------"
add_to_project 5 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-009a: Bicep main"
add_to_project 6 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-009b: Bicep networking"
add_to_project 7 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-009c: Bicep database"
add_to_project 8 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-009d: Bicep containers"
add_to_project 9 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-009e: Bicep deployment"
add_to_project 29 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-008: Log Analytics"
add_to_project 32 "2025-10-01" "2025-10-04" "$DEV1_BACKEND" "POA-011: Database migrations"
add_to_project 34 "2025-10-01" "2025-10-04" "$DEV1_BACKEND" "POA-016: Flutter SDK models"
add_to_project 35 "2025-10-01" "2025-10-04" "$DEV1_BACKEND" "POA-017: Flutter HTTP client"
add_to_project 36 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-018: Docker containers"
add_to_project 37 "2025-10-01" "2025-10-04" "$DEV2_INFRA" "POA-019: Deploy to Azure"
add_to_project 40 "2025-10-01" "2025-10-04" "$DEV1_BACKEND" "POA-034: .NET SDK client"

echo ""
echo "2.2 Week 2 - Features (Oct 7-11) - Development Teams"
echo "-----------------------------------------------------"
add_to_project 13 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-021: OIDC authentication"
add_to_project 15 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-023: Tenant resolution"
add_to_project 16 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-025: CQRS implementation"
add_to_project 17 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-026: Credential issuance API"
add_to_project 27 "2025-10-07" "2025-10-11" "$DEV2_INFRA" "POA-006: Application Gateway"
add_to_project 30 "2025-10-07" "2025-10-11" "$DEV2_INFRA" "POA-009: CI/CD pipelines"
add_to_project 38 "2025-10-07" "2025-10-11" "$BOTH" "POA-020: Week 1 checkpoint"
add_to_project 71 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-150: API rate limiting"

# Add new critical features
add_to_project 78 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-200: Real-time verification"
add_to_project 80 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-203: Admin CRUD"
add_to_project 81 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-204: Selective disclosure"
add_to_project 82 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-205: Revocation system"
add_to_project 83 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-206: Offline verification"

echo ""
echo "2.3 Week 2 - Testing (Oct 7-11) - QA Team"
echo "------------------------------------------"
add_to_project 43 "2025-10-07" "2025-10-11" "$BOTH" "POA-081: Test framework"
add_to_project 44 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-082: Domain tests"
add_to_project 45 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-083: Integration tests"
add_to_project 46 "2025-10-07" "2025-10-11" "$DEV2_INFRA" "POA-084: Test database"
add_to_project 47 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-086: Flutter SDK tests"
add_to_project 48 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-087: Infrastructure tests"
add_to_project 49 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-088: Credential tests"
add_to_project 50 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-089: Auth tests"
add_to_project 51 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-090: API tests"
add_to_project 52 "2025-10-07" "2025-10-11" "$BOTH" "POA-091: Multi-tenant tests"
add_to_project 55 "2025-10-07" "2025-10-11" "$BOTH" "POA-094: Performance tests"
add_to_project 58 "2025-10-07" "2025-10-11" "$BOTH" "POA-081: Test pipeline"

echo ""
echo "2.4 Week 3 - Demo (Oct 14-18) - All Teams"
echo "------------------------------------------"
add_to_project 19 "2025-10-14" "2025-10-18" "$BOTH" "POA-048: Demo presentation"
add_to_project 24 "2025-10-14" "2025-10-18" "$BOTH" "POA-041: Demo mobile app"
add_to_project 79 "2025-10-14" "2025-10-18" "$BOTH" "POA-201: Device deployment"
add_to_project 79 "2025-10-14" "2025-10-18" "$BOTH" "POA-202: Performance dashboard"
add_to_project 57 "2025-10-14" "2025-10-18" "$BOTH" "POA-097: Security testing"
add_to_project 59 "2025-10-14" "2025-10-18" "$BOTH" "POA-097: Security validation"
add_to_project 61 "2025-10-14" "2025-10-18" "$BOTH" "POA-099: Coverage reporting"

echo ""
echo "PHASE 3: TESTING & EVALUATION (Oct 21 - Nov 1)"
echo "==============================================="

echo ""
echo "3.1 Week 4 - UAT Support (Oct 21-25) - Support Team"
echo "----------------------------------------------------"
add_to_project 20 "2025-10-21" "2025-10-25" "$BOTH" "POA-066: Performance testing"
add_to_project 22 "2025-10-21" "2025-10-25" "$BOTH" "POA-066: Performance validation"
add_to_project 25 "2025-10-21" "2025-10-25" "$BOTH" "POA-061: DGov UAT support"
add_to_project 53 "2025-10-21" "2025-10-25" "$BOTH" "POA-092: Security validation"
add_to_project 54 "2025-10-21" "2025-10-25" "$BOTH" "POA-093: Cross-SDK tests"
add_to_project 56 "2025-10-21" "2025-10-25" "$BOTH" "POA-096: Load testing"
add_to_project 60 "2025-10-21" "2025-10-25" "$BOTH" "POA-098: Compliance tests"

echo ""
echo "3.2 Week 5 - Final Evaluation (Oct 28 - Nov 1) - Management"
echo "------------------------------------------------------------"
add_to_project 21 "2025-10-28" "2025-11-01" "$BOTH" "POA-075: Handover package"
add_to_project 23 "2025-10-28" "2025-11-01" "$BOTH" "POA-077: Final presentation"

echo ""
echo "=== PROJECT BOARD SETUP COMPLETE ==="
echo ""
echo "Summary:"
echo "- All issues added to Project #18 in chronological order"
echo "- Start/Target dates align with development phases"
echo "- Resources assigned based on expertise:"
echo "  - Dev1-Backend: Application development, APIs, testing"
echo "  - Dev2-Infra: Infrastructure, DevOps, security"
echo "  - Both: Critical items requiring collaboration"
echo ""
echo "Project Board URL: https://github.com/orgs/Credenxia/projects/18/views/2"
echo ""
echo "Completed at $(date)"