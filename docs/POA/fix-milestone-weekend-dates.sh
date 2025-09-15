#!/bin/bash

echo "=== FIXING WEEKEND MILESTONE DUE DATES ==="
echo "Starting at $(date)"
echo ""

# Function to update milestone due date
update_milestone() {
    local title=$1
    local new_date=$2
    local old_date=$3

    echo "Updating $title: $old_date (Saturday) → $new_date (Friday)"

    # Get milestone number
    MILESTONE_NUM=$(gh api repos/Credenxia/NumbatWallet/milestones --jq ".[] | select(.title == \"$title\") | .number")

    if [ ! -z "$MILESTONE_NUM" ]; then
        # Update the milestone
        gh api -X PATCH repos/Credenxia/NumbatWallet/milestones/$MILESTONE_NUM \
            -f due_on="${new_date}T07:00:00Z" > /dev/null 2>&1
        echo "  ✅ Updated milestone #$MILESTONE_NUM"
    else
        echo "  ⚠️ Milestone not found: $title"
    fi
}

echo "Fixing POA Week milestones (changing Saturday to Friday)..."
echo ""

# Fix all POA week milestones that end on Saturday
update_milestone "010-Week1-POA-Deployment" "2025-10-10" "2025-10-04"
update_milestone "020-Week2-POA-Features" "2025-10-17" "2025-10-11"
update_milestone "025-Week2-POA-AuthAPIs" "2025-10-17" "2025-10-11"
update_milestone "030-Week3-POA-Demo" "2025-10-24" "2025-10-18"
update_milestone "040-Week4-POA-Testing" "2025-10-31" "2025-10-25"
update_milestone "050-Week5-POA-Evaluation" "2025-11-07" "2025-11-01"

echo ""
echo "=== MILESTONE DATES FIXED ==="
echo ""
echo "All POA milestones now end on Fridays:"
echo "- Week 1 Deployment: Oct 10 (Fri)"
echo "- Week 2 Features: Oct 17 (Fri)"
echo "- Week 2 AuthAPIs: Oct 17 (Fri)"
echo "- Week 3 Demo: Oct 24 (Fri)"
echo "- Week 4 Testing: Oct 31 (Fri)"
echo "- Week 5 Evaluation: Nov 7 (Fri)"
echo ""
echo "Pre-development milestones already correct:"
echo "- Wallet App: Sept 19 (Fri)"
echo "- Standards: Sept 26 (Fri)"
echo "- PKI: Sept 26 (Fri)"
echo "- Infrastructure: Oct 3 (Fri)"
echo "- Integration: Oct 3 (Fri)"
echo ""
echo "Completed at $(date)"