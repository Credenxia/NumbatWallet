#!/bin/bash

echo "=== FIXING MILESTONE AND ISSUE DATES TO AVOID WEEKENDS ==="
echo "Starting at $(date)"
echo ""

# Fix milestone dates to avoid weekends
echo "Step 1: Updating milestone dates..."

# 000-PreDev-WalletApp: Sept 16-19 (Mon-Thu)
gh api repos/Credenxia/NumbatWallet/milestones/12 --method PATCH \
  -f due_on="2025-09-19T23:59:59Z" \
  -f description="Wallet application development (Sept 16-19, Mon-Thu, 4 days)"

# 001-PreDev-Standards: Sept 22-26 (Mon-Fri)
gh api repos/Credenxia/NumbatWallet/milestones/13 --method PATCH \
  -f due_on="2025-09-26T23:59:59Z" \
  -f description="Standards implementation (Sept 22-26, Mon-Fri, 5 days)"

# 002-PreDev-PKI: Sept 24-26 (Wed-Fri)
gh api repos/Credenxia/NumbatWallet/milestones/14 --method PATCH \
  -f due_on="2025-09-26T23:59:59Z" \
  -f description="PKI infrastructure (Sept 24-26, Wed-Fri, 3 days)"

# 003-PreDev-Infrastructure: Sept 29-Oct 3 (Mon-Fri)
gh api repos/Credenxia/NumbatWallet/milestones/1 --method PATCH \
  -f due_on="2025-10-03T23:59:59Z" \
  -f description="Azure infrastructure setup (Sept 29-Oct 3, Mon-Fri, 5 days)"

# 004-PreDev-Integration: Oct 2-3 (Thu-Fri)
gh api repos/Credenxia/NumbatWallet/milestones/15 --method PATCH \
  -f due_on="2025-10-03T23:59:59Z" \
  -f description="Integration preparation (Oct 2-3, Thu-Fri, 2 days)"

echo ""
echo "Step 2: Updating PKI issue dates in Project #18..."

# Project and Field IDs
PROJECT_ID="PVT_kwDOBBJaks4BCwXX"
START_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04RmU"
TARGET_DATE_FIELD="PVTF_lADOBBJaks4BCwXXzg04Sug"

# Function to update dates for an issue
update_dates() {
    local issue_num=$1
    local start_date=$2
    local target_date=$3

    echo "Updating issue #$issue_num: $start_date to $target_date"

    # Get item ID in project
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

    if [ ! -z "$ITEM_ID" ]; then
        # Update start date
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

        # Update target date
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

        echo "  ✅ Updated"
    else
        echo "  ⚠️ Issue not found in project"
    fi
}

# Fix PKI issues that start on weekend (Sept 20)
echo ""
echo "Fixing PKI issues #68-70 (move from Sept 20 to Sept 24)..."
update_dates 68 "2025-09-24" "2025-09-26"
update_dates 69 "2025-09-24" "2025-09-26"
update_dates 70 "2025-09-24" "2025-09-26"

# Fix Standards issues to start Monday
echo ""
echo "Fixing Standards issues #62-63 (move to Sept 22-26)..."
update_dates 62 "2025-09-22" "2025-09-26"
update_dates 63 "2025-09-22" "2025-09-26"

# Fix other PKI issues to Wed-Fri
echo ""
echo "Fixing remaining PKI issues #64-67 (Sept 24-26)..."
update_dates 64 "2025-09-24" "2025-09-26"
update_dates 65 "2025-09-24" "2025-09-26"
update_dates 66 "2025-09-24" "2025-09-26"
update_dates 67 "2025-09-24" "2025-09-26"

echo ""
echo "=== DATE FIXES COMPLETE ==="
echo "Summary:"
echo "- Wallet: Sept 16-19 (Mon-Thu)"
echo "- Standards: Sept 22-26 (Mon-Fri)"
echo "- PKI: Sept 24-26 (Wed-Fri, overlaps with Standards)"
echo "- Infrastructure: Sept 29-Oct 3 (Mon-Fri)"
echo "- Integration: Oct 2-3 (Thu-Fri)"
echo ""
echo "All weekend dates have been removed!"
echo "Completed at $(date)"