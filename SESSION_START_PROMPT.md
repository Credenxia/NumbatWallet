# NumbatWallet Development Session Startup Prompt

Use this prompt to start each development session with Claude Code:

---

## 🚀 Session Startup Prompt

**"I'm starting a development session for NumbatWallet backend. Please:**

### 1. Session Initialization
- Review ALL milestones in CLAUDE.md:
  - PreDev milestones (001-PreDev-Standards, 002-PreDev-PKI) - CRITICAL FOUNDATION
  - Backend milestones (011-Backend-Foundation through 017-Backend-Admin)
- Check GitHub Project #18 for current milestone status
- List all open issues for the current and upcoming milestones
- Identify any blocking dependencies between issues
- Check for any failing tests or build warnings from previous session
- **IMPORTANT**: PreDev standards should be established FIRST to avoid refactoring

### 2. Work Planning
- Create a session plan in `/tmp/session_plan_[date].md` with:
  - Issues that can be done in parallel (same milestone, no dependencies)
  - Issues that must be done sequentially (blocking dependencies)
  - Issues already in progress that need completion
  - Estimated time for each task group
- Use TodoWrite to create today's task list
- Identify which issues need to be broken into subtasks
- Group related issues that share code/context

### 3. Development Approach
For each issue/task:
- **TDD MANDATORY**: Write failing tests FIRST, then implementation
- **Zero Warnings**: Fix any compiler warnings immediately
- **Zero Errors**: No broken builds allowed
- Create feature branch: `feature/POA-XXX-description`
- Update issue status to "In Progress" in GitHub Project #18
- If multiple related issues, work on them in same branch

### 4. Progress Tracking
- Update TodoWrite after completing each subtask
- Add progress comments to GitHub issues
- When issue is complete:
  - Run all tests (must pass)
  - Check coverage (must meet requirements)
  - Close issue with completion comment
  - Update TodoWrite to mark as completed

### 5. Session State Check
First, check what was done in previous sessions:
- Review recently closed issues
- Check for any incomplete work (issues marked "In Progress")
- Verify all tests are passing
- Check for any uncommitted changes

### 6. Today's Focus
Based on the milestone dates and dependencies, recommend which issues to tackle today, considering:
- Current date vs milestone due dates
- Issue priorities and blockers
- Optimal parallelization of work
- Related issues that share context

**Start by showing me the current state and proposed work plan.**"

---

## 📋 Quick Commands Reference

### At Session Start:
```bash
# Check current branch and status
git status
git branch

# Run tests to ensure clean state
dotnet test

# Check for build warnings
dotnet build -warnaserror
```

### During Development:
```bash
# TDD Cycle for each feature
dotnet new xunit -n [Feature].Tests  # Create test project if needed
dotnet test --filter [TestName]       # Run specific test (should fail)
# Implement feature
dotnet test --filter [TestName]       # Run test again (should pass)
dotnet test                           # Run all tests

# Check coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Issue Management:
```bash
# Update issue status
gh issue edit [number] --add-label "in-progress"
gh issue comment [number] --body "Started implementation. See PR #XXX"
gh issue close [number] --comment "Completed with tests. Coverage: XX%"

# Check milestone progress
gh issue list --milestone "[milestone-name]" --state open
```

### End of Session:
```bash
# Ensure all tests pass
dotnet test

# Check for warnings
dotnet build -warnaserror

# Commit with issue reference
git add .
git commit -m "POA-XXX: Implement [feature] with TDD

- Added comprehensive tests
- Implementation follows Clean Architecture
- Coverage: XX%

Fixes #XXX"
```

## 🎯 Key Principles to Maintain

1. **TDD is NON-NEGOTIABLE**: Test first, always
2. **Zero Tolerance**: No warnings, no errors, no skipped tests
3. **Clean Architecture**: Respect layer boundaries
4. **Custom CQRS**: No MediatR, use issue #154 pattern
5. **Progress Visibility**: TodoWrite + GitHub issues always updated
6. **Parallel Work**: Maximize efficiency by working on non-blocking issues simultaneously
7. **Session Continuity**: Always check previous session state first

## 💡 Optimization Tips

- **Batch Related Issues**: If issues share domain/context, work on them together
- **Parallel Test Writing**: Write tests for multiple issues, then implement
- **Dependency Chains**: Complete blockers first, then parallelize dependent issues
- **Time Boxing**: Set time limits for complex issues to maintain momentum
- **Early Integration**: Merge completed work frequently to avoid conflicts

---

**Remember**: Each session should move us measurably closer to milestone completion. Use the TodoWrite tool extensively to track progress, and keep GitHub Project #18 updated in real-time.