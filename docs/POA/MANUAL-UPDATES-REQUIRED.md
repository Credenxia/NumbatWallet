# Manual Updates Required in GitHub Project

## Issues Still Showing Weekend Dates

Please manually update these in the GitHub Project view:

### Critical Weekend Fixes

| Issue # | Field | Current | Change To | Task Description |
|---------|-------|---------|-----------|------------------|
| #18 | Target | Oct 11 (Sat) | **Oct 9** | POA-031: Flutter SDK auth |
| #19 | Target | Oct 18 (Sat) | **Oct 17** | POA-048: Demo presentation |
| #22 | Target | Oct 25 (Sat) | **Oct 24** | POA-066: Performance testing |
| #23 | Start & Target | Nov 1 (Sat) | **Oct 31** | POA-077: Final presentation |
| #27 | Start & Target | Oct 4 (Sat) | **Sept 29** | POA-006: App Gateway WAF |
| #30 | Start & Target | Oct 4 (Sat) | **Sept 30** | POA-009: CI/CD pipelines |
| #37 | Start & Target | Oct 4 (Sat) | **Oct 1** | POA-019: Deploy to Container Apps |
| #38 | Start & Target | Oct 4 (Sat) | **Oct 2** | POA-020: Week 1 checkpoint |
| #41 | Target | Oct 18 (Sat) | **Oct 17** | POA-035: TypeScript SDK |
| #55 | Start & Target | Oct 11 (Sat) | **Oct 10** | POA-094: Performance baseline |
| #61 | Start & Target | Oct 18 (Sat) | **Oct 17** | POA-099: Test coverage |

## How to Update in GitHub Project

1. Go to: https://github.com/orgs/Credenxia/projects/18
2. Find each issue in the table
3. Click on the "Start date" field and update
4. Click on the "Target date" field and update
5. Update "Resource" field according to the schedule

## Resource Assignments

- **R1 (Architect/Dev)**: Infrastructure, Auth, Security tasks
- **R2 (Developer)**: Backend, SDKs, Testing tasks
- **Both**: UAT support (#25), Performance testing (#22), Final presentation (#23)

## Complete Schedule Reference

See `/docs/POA/POA-Schedule-Final.md` for the complete schedule with:
- All 61 tasks properly dated
- No weekend work
- Dependencies clearly mapped
- Resources assigned

## Verification

After manual updates, run this check:
```bash
gh project item-list 18 --owner Credenxia --format json --limit 100 | jq -r '.items[] | select(.["start date"] != null or .["target date"] != null) | "\(.content.number) | Start: \(.["start date"]) | Target: \(.["target date"])"' | grep -E "2025-(09-20|09-21|09-27|09-28|10-04|10-05|10-11|10-12|10-18|10-19|10-25|10-26|11-01)"
```

This should return no results if all weekend dates are fixed.