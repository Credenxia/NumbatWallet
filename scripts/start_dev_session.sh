#!/bin/bash

# NumbatWallet Development Session Startup Script
# Run this at the start of each development session

echo "========================================="
echo "NUMBATWALLET DEVELOPMENT SESSION STARTUP"
echo "========================================="
echo "Date: $(date)"
echo

# 1. Git Status Check
echo "1. GIT STATUS"
echo "-------------"
CURRENT_BRANCH=$(git branch --show-current)
echo "Current branch: $CURRENT_BRANCH"

if [ "$CURRENT_BRANCH" != "main" ]; then
    echo "⚠️  Not on main branch. Uncommitted changes:"
    git status --short
else
    echo "✅ On main branch"
fi

# 2. Check for Build Issues
echo
echo "2. BUILD CHECK"
echo "--------------"
if dotnet build -warnaserror > /tmp/build_output.txt 2>&1; then
    echo "✅ Build successful with no warnings"
else
    echo "❌ Build failed or has warnings:"
    cat /tmp/build_output.txt | grep -E "Warning|Error" | head -10
    echo "FIX THESE BEFORE PROCEEDING!"
fi

# 3. Run Tests
echo
echo "3. TEST STATUS"
echo "--------------"
if dotnet test --no-build --nologo -v quiet > /tmp/test_output.txt 2>&1; then
    echo "✅ All tests passing"
    grep -E "Passed:.*Failed:.*Skipped:" /tmp/test_output.txt || echo "Test summary not found"
else
    echo "❌ Tests failing:"
    cat /tmp/test_output.txt | grep -E "Failed|Error" | head -10
    echo "FIX TESTS BEFORE PROCEEDING!"
fi

# 4. Current Milestone Status
echo
echo "4. CURRENT MILESTONE STATUS"
echo "---------------------------"

# Get current date for comparison
CURRENT_DATE=$(date +%Y-%m-%d)

echo "Backend milestones and their status:"
gh api /repos/Credenxia/NumbatWallet/milestones | \
  jq -r '.[] | select(.title | contains("Backend") or contains("IaC") or contains("Admin")) |
  "\(.title) | Due: \(.due_on // "No date") | Open: \(.open_issues) | Closed: \(.closed_issues)"' | \
  while IFS='|' read -r title due open closed; do
    printf "%-30s Due: %-12s Issues: %s/%s\n" "$title" "${due:0:10}" "$closed" "$((open + closed))"
  done

# 5. In-Progress Issues
echo
echo "5. ISSUES IN PROGRESS"
echo "--------------------"
gh issue list --repo Credenxia/NumbatWallet --label "in-progress" --json number,title,assignees | \
  jq -r '.[] | "#\(.number): \(.title)"' || echo "No issues currently in progress"

# 6. Today's Recommended Focus
echo
echo "6. TODAY'S RECOMMENDED FOCUS"
echo "----------------------------"

# Find the nearest upcoming milestone
NEXT_MILESTONE=$(gh api /repos/Credenxia/NumbatWallet/milestones | \
  jq -r --arg date "$CURRENT_DATE" '.[] |
  select(.due_on != null and .due_on >= $date and .open_issues > 0) |
  .title' | head -1)

if [ ! -z "$NEXT_MILESTONE" ]; then
    echo "Focus on milestone: $NEXT_MILESTONE"
    echo
    echo "Open issues for this milestone:"
    gh issue list --repo Credenxia/NumbatWallet --milestone "$NEXT_MILESTONE" --state open --json number,title | \
      jq -r '.[] | "#\(.number): \(.title)"' | head -10
else
    echo "No upcoming milestones with open issues found"
fi

# 7. Create Session Plan
echo
echo "7. SESSION PLAN"
echo "---------------"
SESSION_PLAN="/tmp/session_plan_$(date +%Y%m%d_%H%M%S).md"

cat > "$SESSION_PLAN" << 'EOF'
# Development Session Plan
Date: $(date)

## Current Status
- Branch: $CURRENT_BRANCH
- Tests: [Check above]
- Build: [Check above]

## Today's Goals
[ ] Review blocking dependencies
[ ] Complete in-progress issues
[ ] Start new issues from current milestone

## Issues to Work On
### Parallel Work (can do simultaneously):
- [ ] Issue #XXX: Description
- [ ] Issue #YYY: Description

### Sequential Work (has dependencies):
1. [ ] Issue #AAA: Blocker
2. [ ] Issue #BBB: Depends on AAA

## TDD Checklist for Each Issue
- [ ] Create test file
- [ ] Write failing test
- [ ] Run test (verify it fails)
- [ ] Implement minimal code
- [ ] Run test (verify it passes)
- [ ] Refactor if needed
- [ ] Run all tests
- [ ] Check coverage
- [ ] Commit test + implementation together

## End of Session Checklist
- [ ] All tests passing
- [ ] No build warnings
- [ ] GitHub issues updated
- [ ] TodoWrite list updated
- [ ] Commits reference issue numbers
EOF

echo "Session plan created: $SESSION_PLAN"
echo "Edit this file to customize today's work"

# 8. Reminders
echo
echo "========================================="
echo "REMINDERS"
echo "========================================="
echo "📝 TDD IS MANDATORY - Write tests first!"
echo "⚠️  Zero warnings tolerance - Fix immediately!"
echo "🎯 Update TodoWrite throughout the session"
echo "🔄 Update GitHub issue status when starting/completing"
echo "💡 Check for issues that can be done in parallel"
echo "🏗️  Follow Clean Architecture principles"
echo "🚫 NO MediatR - Use custom CQRS (issue #154)"
echo "========================================="

echo
echo "Ready to start development session!"
echo "Use the prompt in SESSION_START_PROMPT.md with Claude Code"