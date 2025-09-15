#!/bin/bash

echo "=== SETTING UP PROJECT BOARD WITH ALL ISSUES ==="
echo "Project: NumbatWallet POA Phase (#18)"
echo "Starting at $(date)"
echo ""

# Project and Field IDs from the GraphQL query
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

# Status option IDs
TODO="f75ad846"
IN_PROGRESS="47fc9ee4"
DONE="98236657"

echo "Step 1: Getting all open issues..."
ISSUES=$(gh issue list --repo Credenxia/NumbatWallet --state open --limit 200 --json number,title,milestone --jq '.[] | @json')

echo "Step 2: Adding issues to project and setting custom fields..."
echo ""

# Function to add issue to project and set fields
add_to_project() {
    local issue_num=$1
    local title=$2
    local milestone=$3
    local start_date=$4
    local target_date=$5
    local resource=$6

    echo "Processing #$issue_num: $title"

    # First, add the issue to the project
    ITEM_ID=$(gh api graphql -f query="
    mutation {
        addProjectV2ItemById(input: {
            projectId: \"$PROJECT_ID\"
            contentId: \"$(gh api repos/Credenxia/NumbatWallet/issues/$issue_num --jq .node_id)\"
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
                value: { singleSelectOptionId: \"$resource\" }
            }) {
                projectV2Item { id }
            }
        }" > /dev/null 2>&1

        echo "  ✅ Added to project with dates and resource"
    else
        echo "  ⚠️ Could not add to project"
    fi
}

# Process issues by milestone
echo "Processing Pre-Development Wallet Issues..."
for issue in 72 73 74 75 76 77 78 79 80 81 82 83; do
    add_to_project $issue "Wallet/Standards Issue" "000-PreDev-WalletApp" "2025-09-16" "2025-09-19" "$DEV1_BACKEND"
done

echo ""
echo "Processing Pre-Development Standards Issues..."
# Standards issues that should be in Standards milestone
for issue in 62 63; do
    add_to_project $issue "Standards Issue" "000-PreDev-Standards" "2025-09-16" "2025-09-23" "$DEV1_BACKEND"
done

echo ""
echo "Processing Pre-Development PKI Issues..."
for issue in 64 65 66 67 68 69 70 71; do
    add_to_project $issue "PKI Issue" "000-PreDev-PKI" "2025-09-20" "2025-09-25" "$DEV2_INFRA"
done

echo ""
echo "Processing Foundation Issues..."
for issue in 1 2 3 4 26 28 31; do
    add_to_project $issue "Infrastructure Issue" "001-Week0-Foundation" "2025-09-23" "2025-09-27" "$DEV2_INFRA"
done

for issue in 10 11 12 33 39; do
    add_to_project $issue "Backend Issue" "001-Week0-Foundation" "2025-09-23" "2025-09-27" "$DEV1_BACKEND"
done

echo ""
echo "Processing Week 1 Deployment Issues..."
for issue in 5 6 7 8 9 29 32 34 35 36 40; do
    add_to_project $issue "Deployment Issue" "002-Week1-POA-Deployment" "2025-09-30" "2025-10-04" "$BOTH"
done

echo ""
echo "Processing Week 2 Features Issues..."
for issue in 13 14 15 16 17 27 30 37 38; do
    add_to_project $issue "Feature Issue" "003-Week2-POA-Features" "2025-10-07" "2025-10-11" "$DEV1_BACKEND"
done

for issue in 43 44 45 46 47 58; do
    add_to_project $issue "Testing Issue" "003-Week2-POA-Features" "2025-10-07" "2025-10-11" "$DEV1_BACKEND"
done

echo ""
echo "Processing Week 3 Demo Issues..."
for issue in 18 19 24 41 48 49 50 51 52 55 57 59 61; do
    add_to_project $issue "Demo Issue" "004-Week3-POA-Demo" "2025-10-14" "2025-10-18" "$BOTH"
done

echo ""
echo "Processing Week 4-5 Testing/Evaluation Issues..."
for issue in 20 22 25 53 54 56 60; do
    add_to_project $issue "Testing Issue" "005-Week4-POA-Testing" "2025-10-21" "2025-10-25" "$DEV1_BACKEND"
done

for issue in 21 23; do
    add_to_project $issue "Evaluation Issue" "006-Week5-POA-Evaluation" "2025-10-28" "2025-11-01" "$BOTH"
done

echo ""
echo "=== PROJECT BOARD SETUP COMPLETE ==="
echo ""
echo "Summary:"
echo "- All issues added to Project #18"
echo "- Start dates and Target dates set based on milestones"
echo "- Resources assigned (Dev1-Backend, Dev2-Infra, or Both)"
echo "- Issues organized by milestone timeline"
echo ""
echo "Next steps:"
echo "1. Review the project board at: https://github.com/orgs/Credenxia/projects/18"
echo "2. Adjust resource assignments as needed"
echo "3. Update status fields as work progresses"
echo ""
echo "Completed at $(date)"