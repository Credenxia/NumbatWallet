# Generic Development Session Prompt

Use this prompt at the start of any development session to ensure consistent workflow and quality standards.

---

## 🚀 Session Startup Prompt

**"I'm starting a development session. Please:**

### 1. Load Repository Context
- Read CLAUDE.md in the current repository
- Identify repository type and purpose
- Extract key configuration (project number, wiki path, etc.)

### 2. Check Current Work Status
- List active milestones from CLAUDE.md
- Check open issues for current milestone in GitHub
- Identify any blocking dependencies
- Review what was completed vs pending

### 3. Verify Quality Standards
Using the **Quality Checklist** section in CLAUDE.md:
- Run build verification commands
- Ensure ZERO errors and warnings
- Verify all tests are passing
- Check test coverage meets requirements
- Scan for vulnerable packages

### 4. Plan Session Work
- Review open issues without blockers
- Identify tasks that can be done in parallel
- Create work plan using TodoWrite
- Follow **Development Guidelines** from CLAUDE.md

### 5. Apply Development Standards
Follow guidelines from CLAUDE.md:
- Use **Architecture Guidelines** for code organization
- Apply **Development Guidelines** for coding standards
- Follow **TDD Workflow** if specified
- Use proper Git workflow from guidelines

### 6. Session Recommendations
Based on the analysis:
- Prioritize issues by dependencies
- Suggest parallel work opportunities
- Highlight any setup requirements
- Identify potential blockers early

**Show me the current status and proposed work plan.**"

---

## 📋 Workflow Checkpoints

### Before Starting Any Task
1. ✅ Check issue for latest requirements and blockers
2. ✅ Verify prerequisites are complete
3. ✅ Update issue status in GitHub Project
4. ✅ Create feature branch per Git workflow

### During Development
1. ✅ Follow TDD cycle from Development Guidelines
2. ✅ Run quality checks frequently
3. ✅ Update TodoWrite progress
4. ✅ Comment on GitHub issue with updates

### Before Committing
1. ✅ Run all commands from Quality Checklist
2. ✅ Verify coverage meets requirements
3. ✅ Ensure zero errors/warnings
4. ✅ Follow commit message format

### Session End Checklist
1. ✅ All tests passing
2. ✅ No warnings or errors
3. ✅ Coverage target met
4. ✅ Work committed or stashed
5. ✅ GitHub issues updated
6. ✅ TodoWrite tasks completed

## 🔄 Continuous Quality Loop

```
1. CHECK: Run quality verification
   └─> Errors/Warnings? → FIX → CHECK

2. TEST: Verify all tests pass
   └─> Failures? → FIX → TEST

3. COVERAGE: Check coverage percentage
   └─> Below target? → ADD TESTS → COVERAGE

4. SECURITY: Scan for vulnerabilities
   └─> Issues found? → UPDATE → SECURITY

5. COMMIT: All checks passed ✅
```

## 🎯 Key References in CLAUDE.md

Look for these sections in CLAUDE.md:
- **Quick Start**: Language, architecture, testing requirements
- **Quality Checklist**: Zero-tolerance standards and verification
- **Development Guidelines**: TDD workflow, coding standards
- **Architecture Guidelines**: Clean architecture rules, patterns
- **Active Milestones**: Current work and priorities
- **Quick Commands**: Language-specific build/test commands
- **GitHub Project**: Project number and issue tracking

## 💡 Repository Detection

The prompt adapts based on CLAUDE.md content:
- Contains `.NET/C#` info → Apply C# standards
- Contains `Flutter` info → Apply Dart standards
- Contains `TypeScript` info → Apply JS/TS standards
- Contains `Infrastructure` info → Apply IaC standards

## 🚦 Success Criteria

Session is properly initialized when:
1. ✅ CLAUDE.md context loaded
2. ✅ Quality standards verified (zero errors/warnings)
3. ✅ Current work status understood
4. ✅ Work plan created with TodoWrite
5. ✅ All checks passing

---

**Important**: This prompt orchestrates the workflow. All technical details, commands, and standards are defined in each repository's CLAUDE.md file.

*Version: 2.0 | Created: September 2025 | Workflow-focused, repository-agnostic*