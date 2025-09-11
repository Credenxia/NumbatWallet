# GitHub Project Date Update Guide

## Overview
This guide documents how to programmatically update GitHub Project (v2) fields, specifically date fields, using the GitHub GraphQL API.

## Prerequisites
- GitHub CLI (`gh`) installed and authenticated
- Project admin permissions
- Project ID and field IDs

## Finding Project and Field IDs

### 1. Get Project ID
```bash
gh api graphql -f query='
query {
  organization(login: "YOUR_ORG") {
    projectV2(number: PROJECT_NUMBER) {
      id
    }
  }
}'
```

### 2. Get Field IDs
```bash
gh api graphql -f query='
query {
  organization(login: "YOUR_ORG") {
    projectV2(number: PROJECT_NUMBER) {
      fields(first: 20) {
        nodes {
          ... on ProjectV2Field {
            id
            name
          }
        }
      }
    }
  }
}'
```

## Updating Date Fields

### GraphQL Mutation Structure
```graphql
mutation {
  updateProjectV2ItemFieldValue(input: {
    projectId: "PROJECT_ID"
    itemId: "ITEM_ID"
    fieldId: "FIELD_ID"
    value: {date: "YYYY-MM-DD"}
  }) {
    projectV2Item {
      id
    }
  }
}
```

### Bash Function Example
```bash
update_dates() {
    local item_id=$1
    local start_date=$2
    local target_date=$3
    
    # Update start date
    gh api graphql -f query="
    mutation {
      updateProjectV2ItemFieldValue(input: {
        projectId: \"$PROJECT_ID\"
        itemId: \"$item_id\"
        fieldId: \"$START_FIELD_ID\"
        value: {date: \"$start_date\"}
      }) {
        projectV2Item {
          id
        }
      }
    }"
    
    # Update target date
    gh api graphql -f query="
    mutation {
      updateProjectV2ItemFieldValue(input: {
        projectId: \"$PROJECT_ID\"
        itemId: \"$item_id\"
        fieldId: \"$TARGET_FIELD_ID\"
        value: {date: \"$target_date\"}
      }) {
        projectV2Item {
          id
        }
      }
    }"
}
```

## Getting All Project Items

### Query to Fetch Items with Issue Numbers
```bash
gh api graphql -f query='
query {
  organization(login: "YOUR_ORG") {
    projectV2(number: PROJECT_NUMBER) {
      items(first: 100) {
        nodes {
          id
          content {
            ... on Issue {
              number
              title
            }
          }
        }
      }
    }
  }
}'
```

## Complete Update Process

### 1. Fetch All Items
```bash
ITEMS=$(gh api graphql -f query='...' | jq -r '.data.organization.projectV2.items.nodes[] | "\(.content.number):\(.id)"')
```

### 2. Create Mapping
```bash
declare -A ITEM_IDS
while IFS=: read -r num id; do
    ITEM_IDS[$num]=$id
done <<< "$ITEMS"
```

### 3. Update Each Item
```bash
update_dates "${ITEM_IDS[1]}" "2025-09-15" "2025-09-15" 1
```

## Best Practices

### 1. Rate Limiting
- Add delays between updates if updating many items
- Check rate limit status: `gh api rate_limit`

### 2. Error Handling
```bash
update_dates() {
    # ... update logic ...
    
    if [ $? -eq 0 ]; then
        echo "✅ Updated"
    else
        echo "❌ Failed to update"
    fi
}
```

### 3. Validation
- Always verify dates don't fall on weekends
- Check for dependency conflicts
- Validate inclusive date ranges

### 4. Batch Updates
- Group updates by week or milestone
- Use transactions where possible
- Log all changes for audit

## Common Issues and Solutions

### Issue: API Rate Limit
**Solution:** Add delays between calls or use batch operations

### Issue: Items Not Found
**Solution:** Ensure issues are added to project first:
```bash
gh project item-add PROJECT_NUMBER --owner ORG --url ISSUE_URL
```

### Issue: Permission Denied
**Solution:** Verify project admin permissions and token scopes

## POA Specific Implementation

For the NumbatWallet POA project:
- **Project ID:** PVT_kwDOBBJaks4BCwXX
- **Start Date Field:** PVTF_lADOBBJaks4BCwXXzg04RmU
- **Target Date Field:** PVTF_lADOBBJaks4BCwXXzg04Sug
- **Resource Field:** PVTSSF_lADOBBJaks4BCwXXzg05B2s

## References
- [GitHub GraphQL API Docs](https://docs.github.com/en/graphql)
- [GitHub Projects v2 API](https://docs.github.com/en/issues/planning-and-tracking-with-projects/automating-your-project/using-the-api-to-manage-projects)
- [GitHub CLI Manual](https://cli.github.com/manual/)