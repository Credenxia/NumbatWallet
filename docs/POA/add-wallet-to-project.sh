#!/bin/bash

echo "=== ADDING WALLET ISSUES TO PROJECT #18 ==="
echo "Starting at $(date)"
echo ""

# Project and Field IDs
PROJECT_ID="PVT_kwDOBBJaks4BCwXX"
START_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04RmU"
TARGET_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04Sug"
RESOURCE_FIELD="PVTSSF_lADOBBJaks4BCwXXzg05B2s"

# Resource option ID for Dev1-Backend (mobile team)
DEV1_BACKEND="af6cd89f"

# Function to add issue to project and set dates
add_to_project() {
    local issue_num=$1
    local start_date=$2
    local target_date=$3
    local title=$4

    echo "Adding issue #$issue_num: $title"

    # Get issue node ID
    ISSUE_NODE_ID=$(gh api repos/Credenxia/NumbatWallet/issues/$issue_num --jq .node_id 2>/dev/null)

    if [ -z "$ISSUE_NODE_ID" ]; then
        echo "  ⚠️ Issue #$issue_num not found"
        return
    fi

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

    if [ -z "$ITEM_ID" ]; then
        # Issue might already be in project, try to get its ID
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
        # Set Start Date
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
        }" > /dev/null 2>&1

        # Set Target Date
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
        }" > /dev/null 2>&1

        # Set Resource
        gh api graphql -f query="
        mutation {
            updateProjectV2ItemFieldValue(input: {
                projectId: \"$PROJECT_ID\"
                itemId: \"$ITEM_ID\"
                fieldId: \"$RESOURCE_FIELD\"
                value: { singleSelectOptionId: \"$DEV1_BACKEND\" }
            }) {
                projectV2Item { id }
            }
        }" > /dev/null 2>&1

        echo "  ✅ Added to project with dates and resource"
    else
        echo "  ⚠️ Could not add to project"
    fi
}

# Add wallet issues with proper dates (Sept 16-19, Mon-Thu)
echo "Adding wallet application issues..."
add_to_project 72 "2025-09-16" "2025-09-19" "POA-100: Architecture"
add_to_project 73 "2025-09-16" "2025-09-17" "POA-101: UI/UX designs"
add_to_project 74 "2025-09-17" "2025-09-18" "POA-102: Onboarding"
add_to_project 75 "2025-09-17" "2025-09-18" "POA-103: Dashboard"
add_to_project 76 "2025-09-18" "2025-09-19" "POA-104: List view"
add_to_project 77 "2025-09-18" "2025-09-19" "POA-105: Detail screen"
add_to_project 78 "2025-09-18" "2025-09-19" "POA-106: Sharing UI"
add_to_project 79 "2025-09-17" "2025-09-18" "POA-107: Biometric"
add_to_project 80 "2025-09-18" "2025-09-19" "POA-108: Notifications"
add_to_project 81 "2025-09-19" "2025-09-19" "POA-109: Offline mode"
add_to_project 82 "2025-09-19" "2025-09-19" "POA-110: Settings"
add_to_project 83 "2025-09-18" "2025-09-19" "POA-111: Backup"
add_to_project 84 "2025-09-16" "2025-09-17" "POA-112: State mgmt"
add_to_project 85 "2025-09-19" "2025-09-19" "POA-113: Test suite"

echo ""
echo "=== WALLET ISSUES ADDED TO PROJECT ==="
echo "All issues added with:"
echo "- Start/Target dates (Sept 16-19)"
echo "- Resource: Dev1-Backend (Mobile team)"
echo ""
echo "Completed at $(date)"