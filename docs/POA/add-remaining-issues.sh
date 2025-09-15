#!/bin/bash

echo "=== ADDING REMAINING ISSUES TO PROJECT #18 ==="
echo "Starting at $(date)"
echo ""

# Project and Field IDs
PROJECT_ID="PVT_kwDOBBJaks4BCwXX"
START_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04RmU"
TARGET_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04Sug"
RESOURCE_FIELD="PVTSSF_lADOBBJaks4BCwXXzg05B2s"

# Resource option IDs
DEV1_BACKEND="af6cd89f"
DEV2_INFRA="51a08165"
BOTH="db524408"

# Function to add issue to project with fields
add_to_project() {
    local issue_num=$1
    local start_date=$2
    local target_date=$3
    local resource=$4
    local description=$5

    echo "Adding #$issue_num: $description"

    # Get issue node ID
    ISSUE_NODE_ID=$(gh api repos/Credenxia/NumbatWallet/issues/$issue_num --jq .node_id)

    # Add to project
    ITEM_ID=$(gh api graphql -f query="
    mutation {
        addProjectV2ItemById(input: {
            projectId: \"$PROJECT_ID\"
            contentId: \"$ISSUE_NODE_ID\"
        }) {
            item {
                id
            }
        }
    }" --jq '.data.addProjectV2ItemById.item.id' 2>/dev/null)

    # If not added, try to get existing ID
    if [ -z "$ITEM_ID" ]; then
        ITEM_ID=$(gh api graphql -f query="
        {
            repository(owner: \"Credenxia\", name: \"NumbatWallet\") {
                issue(number: $issue_num) {
                    projectItems(first: 10) {
                        nodes {
                            id
                            project {
                                id
                            }
                        }
                    }
                }
            }
        }" --jq ".data.repository.issue.projectItems.nodes[] | select(.project.id == \"$PROJECT_ID\") | .id" 2>/dev/null)
    fi

    if [ ! -z "$ITEM_ID" ]; then
        # Set all fields in parallel
        gh api graphql -f query="
        mutation {
            updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$START_DATE_FIELD\"
                value: { date: \"$start_date\" }
            }) {
                projectV2Item { id }
            }
        }" > /dev/null 2>&1 &

        gh api graphql -f query="
        mutation {
            updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$TARGET_DATE_FIELD\"
                value: { date: \"$target_date\" }
            }) {
                projectV2Item { id }
            }
        }" > /dev/null 2>&1 &

        gh api graphql -f query="
        mutation {
            updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$RESOURCE_FIELD\"
                value: { singleSelectOptionId: \"$resource\" }
            }) {
                projectV2Item { id }
            }
        }" > /dev/null 2>&1 &

        wait
        echo "  ✅ Added with custom fields"
    else
        echo "  ⚠️ Could not add to project"
    fi
}

echo "Adding PKI issues that were missing..."
add_to_project 71 "2025-09-20" "2025-09-25" "$DEV2_INFRA" "POA-132: PKI audit logging"

echo ""
echo "Adding wallet issues #78-83..."
add_to_project 78 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-102: Wallet home screen"
add_to_project 79 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-103: Credential list view"
add_to_project 80 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-104: Credential details"
add_to_project 81 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-105: Share/present screen"
add_to_project 82 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-106: Settings screen"
add_to_project 83 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-107: Biometric auth"

echo ""
echo "Adding wallet issues #84-86..."
add_to_project 84 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-108: Push notifications"
add_to_project 85 "2025-09-16" "2025-09-19" "$DEV1_BACKEND" "POA-109: Offline mode"
add_to_project 86 "2025-09-27" "2025-09-30" "$BOTH" "ServiceWA integration POC"

echo ""
echo "Adding admin portal issues #87-91..."
add_to_project 87 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-133: Admin dashboard UI"
add_to_project 88 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-134: Credential management"
add_to_project 89 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-135: User management"
add_to_project 90 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-136: Analytics dashboard"
add_to_project 91 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-137: Audit logs viewer"

echo ""
echo "Adding feature issues #92-109..."
add_to_project 92 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-138: QR code generation"
add_to_project 93 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-139: QR code scanning"
add_to_project 94 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-140: Offline verification"
add_to_project 95 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-141: NFC communication"
add_to_project 96 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-142: Bluetooth LE"
add_to_project 97 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-143: Real-time updates"
add_to_project 98 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-144: Biometric binding"
add_to_project 99 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-145: Multi-device sync"
add_to_project 100 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-146: Backup and recovery"
add_to_project 101 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-147: PIN management"
add_to_project 102 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-148: Emergency access"
add_to_project 103 "2025-10-07" "2025-10-11" "$DEV1_BACKEND" "POA-149: Privacy controls"
add_to_project 104 "2025-10-14" "2025-10-18" "$BOTH" "POA-150: Performance testing"
add_to_project 105 "2025-10-14" "2025-10-18" "$BOTH" "POA-151: Load testing"
add_to_project 106 "2025-10-14" "2025-10-18" "$BOTH" "POA-152: Security testing"
add_to_project 107 "2025-10-14" "2025-10-18" "$BOTH" "End-to-end demo prep"
add_to_project 108 "2025-10-14" "2025-10-18" "$BOTH" "Device provisioning"
add_to_project 109 "2025-10-14" "2025-10-18" "$BOTH" "Demo scenarios testing"

echo ""
echo "Adding standards issues #121-124..."
add_to_project 121 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "POA-121: Verifiable presentations"
add_to_project 122 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "POA-122: Proof of possession"
add_to_project 123 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "POA-123: Status list 2021"
add_to_project 124 "2025-09-16" "2025-09-23" "$DEV1_BACKEND" "POA-124: Bitstring status"

echo ""
echo "=== COMPLETE ==="
echo "All remaining issues added to Project #18"
echo "Completed at $(date)"