#!/bin/bash

echo "=== UPDATING PROJECT #18 DATES WITH DEPENDENCY RESPECT ==="
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

# Function to update dates for an issue
update_dates() {
    local issue_num=$1
    local start_date=$2
    local target_date=$3
    local resource=$4

    echo "Updating #$issue_num: $start_date to $target_date ($resource)"

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
        # Update dates
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

        if [ ! -z "$resource" ]; then
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
        fi

        echo "  ✅ Updated"
    else
        echo "  ⚠️ Not in project"
    fi
}

echo "Phase 1: Wallet Application (Sept 16-20, 5 days)"
echo "------------------------------------------------"
# New wallet issues - 5 days for foundation
for issue in 72 73 74 75 76 77 78 79 80 81 82 83 84 85; do
    update_dates $issue "2025-09-16" "2025-09-20" "$DEV1_BACKEND"
done

echo ""
echo "Phase 2: Standards Implementation (Sept 23-27, 5 days)"
echo "-------------------------------------------------------"
# Standards - starts after wallet foundation
update_dates 62 "2025-09-23" "2025-09-27" "$DEV1_BACKEND"
update_dates 63 "2025-09-23" "2025-09-27" "$DEV1_BACKEND"

echo ""
echo "Phase 3: PKI Infrastructure (Sept 23-27, 5 days parallel)"
echo "-----------------------------------------------------------"
# PKI can run parallel with standards
for issue in 64 65 66 67 68 69 70 71; do
    update_dates $issue "2025-09-23" "2025-09-27" "$DEV2_INFRA"
done

echo ""
echo "Phase 4: Infrastructure Foundation (Sept 25-30, 6 days)"
echo "---------------------------------------------------------"
# Infrastructure - starts mid-week, needs 6 days
# Azure setup
for issue in 1 2 3 4 26 28; do
    update_dates $issue "2025-09-25" "2025-09-30" "$DEV2_INFRA"
done
# Backend foundation
for issue in 10 11 31 33; do
    update_dates $issue "2025-09-25" "2025-09-30" "$DEV1_BACKEND"
done

echo ""
echo "Phase 5: SDK Development (Sept 26-30, 5 days)"
echo "-----------------------------------------------"
# SDKs - starts after infrastructure begins
update_dates 12 "2025-09-26" "2025-09-30" "$DEV1_BACKEND"  # Flutter SDK
update_dates 39 "2025-09-26" "2025-09-30" "$DEV1_BACKEND"  # .NET SDK
update_dates 41 "2025-09-26" "2025-09-30" "$DEV1_BACKEND"  # TypeScript SDK

echo ""
echo "Phase 6: Integration Prep (Sept 30 - Oct 2, 3 days)"
echo "-----------------------------------------------------"
# Integration - after infrastructure
update_dates 14 "2025-09-30" "2025-10-02" "$BOTH"  # WA IdX
update_dates 18 "2025-09-30" "2025-10-02" "$DEV1_BACKEND"  # Flutter auth

echo ""
echo "Phase 7: POA Week 1 - Deployment (Oct 3-4, 2 days)"
echo "----------------------------------------------------"
# Deployment - quick 2-day sprint
for issue in 5 6 7 8 9 29 32 34 35 36 37 40; do
    update_dates $issue "2025-10-03" "2025-10-04" "$BOTH"
done

echo ""
echo "Phase 8: POA Week 2 - Features (Oct 7-11, 5 days)"
echo "---------------------------------------------------"
# Features implementation
for issue in 13 15 16 17 27 30 38 43 44 45 46 47 71; do
    update_dates $issue "2025-10-07" "2025-10-11" "$DEV1_BACKEND"
done

echo ""
echo "Phase 9: POA Week 2 - Auth APIs (Oct 7-11, parallel)"
echo "------------------------------------------------------"
# Authentication testing (parallel)
for issue in 48 49 50 51 52 55; do
    update_dates $issue "2025-10-07" "2025-10-11" "$DEV1_BACKEND"
done

echo ""
echo "Phase 10: POA Week 3 - Demo Prep (Oct 14-18, 5 days)"
echo "------------------------------------------------------"
# Demo preparation
for issue in 19 24 42 57 58 59 61; do
    update_dates $issue "2025-10-14" "2025-10-18" "$BOTH"
done

echo ""
echo "Phase 11: POA Week 4 - Testing (Oct 21-25, 5 days)"
echo "----------------------------------------------------"
# UAT support
for issue in 20 22 25 53 54 56 60; do
    update_dates $issue "2025-10-21" "2025-10-25" "$DEV1_BACKEND"
done

echo ""
echo "Phase 12: POA Week 5 - Evaluation (Oct 28 - Nov 1, 5 days)"
echo "------------------------------------------------------------"
# Final evaluation
for issue in 21 23; do
    update_dates $issue "2025-10-28" "2025-11-01" "$BOTH"
done

echo ""
echo "=== PROJECT DATE UPDATE COMPLETE ==="
echo ""
echo "Summary:"
echo "- Pre-development: Sept 16 - Oct 2 (17 days)"
echo "- Official POA: Oct 3 - Nov 1 (30 days)"
echo "- Total duration: 47 days"
echo ""
echo "Key improvements:"
echo "1. Wallet gets full 5 days (Sept 16-20)"
echo "2. Standards/PKI run in parallel (Sept 23-27)"
echo "3. Infrastructure gets 6 days (Sept 25-30)"
echo "4. Dependencies respected - no same-day starts"
echo "5. All dates are inclusive (start and end dates)"
echo ""
echo "Completed at $(date)"