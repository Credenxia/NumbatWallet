# GitHub Project Template & Guidelines

**Version:** 1.0  
**Date:** September 10, 2025  
**Purpose:** Standardize GitHub project management for NumbatWallet

## Milestone Naming Convention

### Format
```
XXX-[Category]-[Name]-[Version]
```

### Order Prefix Rules
- **001-010**: Primary POA milestones (weekly/phase-based)
- **011-020**: SDK milestones
- **021-030**: Infrastructure milestones
- **031-040**: Backend/API milestones
- **041-050**: Testing milestones
- **051-060**: Documentation milestones
- **061-070**: Security milestones
- **071-080**: Performance milestones
- **081-090**: Deployment milestones
- **091-100**: Support/maintenance milestones

### Current POA Milestones (Ordered)
```
001-POA-Foundation             (Oct 1-3, 2025)   - Infrastructure setup and SDK delivery
002-SDK-Flutter-v1             (Oct 1-3, 2025)   - Flutter SDK for ServiceWA with tests
003-Infra-Azure-Setup          (Oct 1-3, 2025)   - Azure environment and test infrastructure
004-Backend-Core-API-v1        (Oct 6-10, 2025)  - Core API implementation with tests
005-SDK-DotNet-v1              (Oct 6-10, 2025)  - .NET SDK for agencies
006-POA-Integration            (Oct 6-10, 2025)  - Authentication and credential operations  
007-POA-Demo                   (Oct 13-17, 2025) - Feature completion and demonstration
008-SDK-TypeScript-v1          (Oct 13-17, 2025) - TypeScript SDK for web
009-POA-Testing                (Oct 20-24, 2025) - DGov testing support
010-POA-Evaluation             (Oct 27-31, 2025) - Final evaluation and handover
```

### Test Issue Distribution
- **Infrastructure (003)**: Test framework, CI/CD, test database (#43, #45, #46, #58)
- **Backend API (004)**: Domain tests, API tests, auth tests (#44, #48-52)
- **Flutter SDK (002)**: Flutter unit tests (#47)
- **Integration (006)**: Security, cross-SDK, performance (#53-55)
- **Demo (007)**: E2E, load, penetration testing (#56-61)

## Project Configuration

### Required Custom Fields
1. **Start date** (Date field)
   - Field ID: `PVTF_lADOBBJaks4BCwXXzg04RmU`
   - Used for task start date
   - Required for roadmap view

2. **Target date** (Date field)
   - Field ID: `PVTF_lADOBBJaks4BCwXXzg04Sug`
   - Used for task end date
   - Required for roadmap view

3. **Resource** (Single-select field)
   - Field ID: `PVTSSF_lADOBBJaks4BCwXXzg05B2s`
   - Used for resource allocation without assignees
   - Options:
     - **Dev1-Backend** (Blue): Backend, .NET SDK, TypeScript SDK
     - **Dev2-Infra** (Green): Azure setup, then Integration
     - **Both** (Purple): Collaborative tasks
     - **Unassigned** (Gray): Not yet assigned

### Project Views
1. **Board View** (Default)
   - Columns: Backlog, Ready, In Progress, In Review, Testing, Done
   - Group by: Status
   - Sort by: Priority

2. **Roadmap View**
   - Timeline: October 1 - November 4, 2025
   - Group by: Milestone
   - Date fields: Start date, Target date
   - Zoom: Week view

3. **Table View**
   - Columns: Issue, Assignee, Status, Priority, Size, Milestone, Start date, Target date
   - Sort by: Start date (ascending)
   - Filter: Label contains "poa"

## Issue Template

### Standard Issue Fields
```markdown
## Description
[Clear description of the task]

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Technical Notes
[Implementation details]

## Testing Requirements
[TDD requirements - tests to write first]

## Dependencies
- Depends on: #XX
- Blocks: #YY

## Definition of Done
- [ ] Tests written and passing
- [ ] Code implemented (TDD)
- [ ] Documentation updated
- [ ] PR approved and merged
```

### Required Labels
- Priority: `P0`, `P1`, `P2`, `P3`
- Size: `XS`, `S`, `M`, `L`, `XL`
- Category: `infrastructure`, `backend`, `frontend`, `testing`, `documentation`
- Type: `feature`, `bug`, `enhancement`, `testing`
- Phase: `poa`, `pilot`, `production`

## Automation Rules

### GitHub Actions Automation
```yaml
name: Project Automation

on:
  issues:
    types: [opened, closed, assigned]
  pull_request:
    types: [opened, closed, review_requested]

jobs:
  project-automation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/add-to-project@v0.5.0
        with:
          project-url: https://github.com/orgs/Credenxia/projects/18
          github-token: ${{ secrets.PROJECT_TOKEN }}
```

### Project Board Automation
- When issue created → Add to Backlog
- When assignee added → Move to Ready
- When PR linked → Move to In Review
- When PR merged → Move to Testing
- When issue closed → Move to Done

## Creating New Milestones

### Step-by-Step Process
1. **Determine order prefix** based on category (see rules above)
2. **Create milestone** with format: `XXX-Category-Name`
3. **Set due date** aligned with project timeline
4. **Add description** with clear deliverables
5. **Link to project** #18

### Example Command
```bash
gh api repos/Credenxia/NumbatWallet/milestones \
  -X POST \
  -f title="041-Testing-Coverage-v1" \
  -f description="Achieve 90% test coverage across all components" \
  -f due_on="2025-10-18T23:59:59Z" \
  -f state="open"
```

## Issue Dependencies

### Using Tasklists for Dependencies
```markdown
## Blocks
- [ ] #24
- [ ] #25
- [ ] #26
```

This creates `trackedIssues` relationships visible in:
- Issue relationship section
- Project roadmap dependency lines
- GraphQL API queries

### Checking Dependencies
```bash
# Check what an issue blocks
gh api graphql -f query='
query {
  repository(owner: "Credenxia", name: "NumbatWallet") {
    issue(number: 1) {
      trackedIssues(first: 10) {
        nodes { number title }
      }
    }
  }
}'

# Check what blocks an issue
gh api graphql -f query='
query {
  repository(owner: "Credenxia", name: "NumbatWallet") {
    issue(number: 2) {
      trackedInIssues(first: 10) {
        nodes { number title }
      }
    }
  }
}'
```

## Resource Allocation Strategy

### Development Team Structure
With 2 developers working in parallel:

**Dev1-Backend (Backend Focus):**
- Week 1 (Oct 1-3): Backend Core API setup, domain models
- Week 2 (Oct 6-10): .NET SDK development for agencies
- Week 3 (Oct 13-17): TypeScript/JavaScript SDK for web
- Week 4-5 (Oct 20-31): Support testing and fixes

**Dev2-Infra (Infrastructure Focus):**
- Week 1 (Oct 1-3): Azure infrastructure setup, CI/CD
- Week 2 (Oct 6-10): Integration work, authentication flows  
- Week 3 (Oct 13-17): Demo preparation, deployment
- Week 4-5 (Oct 20-31): Performance and security testing

**Collaborative Work (Both):**
- Flutter SDK development (mobile expertise needed)
- Demo features and UI components
- Final testing and evaluation

### Resource Assignment Without Assignees
Since team members are not yet onboarded, use the Resource field to:
1. Plan workload distribution
2. Identify parallel work streams
3. Spot resource conflicts early
4. Balance tasks between developers

## Reporting

### Weekly Status Query
```bash
# Get issues by milestone
gh issue list --milestone "001-POA-Week1-Foundation" \
  --json number,title,state,assignees \
  --jq '.[] | "\(.number): \(.title) [\(.state)] - \(.assignees[0].login // "unassigned")"'

# Get project progress
gh api graphql -f query='
query {
  node(id: "PVT_kwDOBBJaks4BCwXX") {
    ... on ProjectV2 {
      items(first: 100) {
        totalCount
        nodes {
          fieldValues(first: 10) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                field {
                  ... on ProjectV2SingleSelectField {
                    name
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}' | jq '.data.node.items.nodes[].fieldValues.nodes[] | select(.field.name == "Status") | .name' | sort | uniq -c
```

## Best Practices

### Daily Workflow
1. **Morning**: Check assigned issues in project board
2. **Before coding**: Write tests first (TDD)
3. **During work**: Update issue comments with progress
4. **End of day**: Move cards to appropriate columns

### PR Linking
- Always reference issue: `Fixes #XX` or `Relates to #XX`
- Link PR to project automatically via GitHub Actions
- Ensure CI passes before requesting review

### Milestone Management
- Close milestones only when ALL issues complete
- Review milestone progress weekly
- Adjust dates if needed (document reason)

## Troubleshooting

### Common Issues

#### Issue not showing in roadmap
- Check if Start date and Target date are set
- Verify issue is added to project
- Ensure date format is YYYY-MM-DD

#### Dependencies not visible
- Use tasklist format with `- [ ] #XX`
- Check GraphQL for `trackedIssues`
- Verify issue numbers are correct

#### Milestone order incorrect
- Check order prefix (XXX-)
- Sort by title in milestone list
- Update prefix if needed

## Quick Reference

### Project IDs
- Project ID: `PVT_kwDOBBJaks4BCwXX`
- Start Date Field: `PVTF_lADOBBJaks4BCwXXzg04RmU`
- Target Date Field: `PVTF_lADOBBJaks4BCwXXzg04Sug`
- Resource Field: `PVTSSF_lADOBBJaks4BCwXXzg05B2s`

### Useful Commands
```bash
# Add issue to project
gh issue edit XX --add-project "NumbatWallet POA Phase"

# Set milestone
gh issue edit XX --milestone "001-POA-Week1-Foundation"

# Add labels
gh issue edit XX --add-label "testing,P0,M"

# List all POA issues
gh issue list --label "poa" --limit 100

# View project board
gh project view 18
```

---

**Note**: This template should be used for all future GitHub project management tasks. Update CLAUDE.md to reference this document.