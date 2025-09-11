# GitHub Projects Template (New Development) — **Full Guide**
> **Audience:** Engineers, Product, QA — written so a **junior analyst** can follow step-by-step.  
> **Hierarchy:** **Project** → **Milestones** → **Issues** (with optional **Subtasks**).  
> **Goal:** Make work visible, unblocked, **ordered**, and easy for humans & AI to pick the next task.

---

## 0) Quick Start (5 minutes)
1) **Create/choose the Project** (e.g., *Admin Portal — Web*).  
2) **Create Milestone(s)** using the naming rules (Versioned or Outcome-Based).  
3) **Create Issues** via templates; assign each to **Project + Milestone + Owner**.  
4) Add **Dependencies** (what it waits on) and **Subtasks** (how we’ll do it).  
5) Set **PO/MO/IO** (Project/Milestone/Issue Order numbers).  
6) When the **Ready Gate** is met, add `ready-to-code` and set **Status → Ready**.  
7) Use the **Next Up** view to start work. Parallel items share the same order number.

---

## 1) Naming & Conventions (Projects, Milestones, Versions)
### 1.1 Project names
**Format:** `<Area> — <Module>`  
**Examples:** `Core Backend — Architecture`, `Admin Portal — Web`, `Client Portal — Web`, `Public API — External`

### 1.2 Milestone names — **Version vs Outcome** (READ THIS)
Pick **one style per project** and be consistent.

#### A) **Versioned Milestone** (ship by itself)
**Format:** `<ProjectShort>-<Scope> vX.Y`  
**Examples:** `CB-Application v0.1`, `AP-Menu v0.2`, `API-Contract v1.0`  
**Use when:** Milestone maps to a **git tag/release**, you publish **release notes**, or multiple modules align to the same version.  
**Why:** Traceable **Milestone → Release tag → Deployment**; communicates **maturity** (MVP v0.1, GA v1.0).

#### B) **Outcome-Based Milestone** (bundled with others)
**Format:** `<ProjectShort>-<Outcome>` *(no version in name)*  
**Examples:** `AP-Menu — Persist State`, `AP-Theme — Tenant Overrides`, `CB-Domain — Events`  
Set the **Target Release** field (e.g., `v0.3`).  
**Use when:** Work won’t ship alone; you want plain-English outcomes.

**Decision helper:** Can we ship this milestone by itself? → **Yes:** Versioned. **No:** Outcome + *Target Release*.  
> **Team default:** If unsure, use **Outcome + Target Release** (clearest to juniors).

### 1.3 Labels & other conventions
- **Type:** `type:feature`, `type:bug`, `type:tech-debt`, `type:spike`  
- **Area:** `area:core-backend`, `area:admin-portal`, `area:client-portal`, `area:api`  
- **Priority:** `P0`, `P1`, `P2`, `P3` (*P0 = must fix now*)  
- **Size:** `XS`, `S`, `M`, `L`, `XL` (*rough effort*)  
- **Flags:** `blocked`, `ready-to-code`, `security-review`, `docs-needed`, `qa-needed`  
- **Branches:** `feature/<short-scope>`, `bugfix/<short-scope>`, `spike/<short-scope>`  
- **Commits/PRs:** use `Fixes #123` / `Closes #456`

### 1.4 **Development Order** (PO/MO/IO)
Add three **Number** fields in the Project:
- **PO (Project Order):** order of modules (lower = earlier)
- **MO (Milestone Order):** order within a project
- **IO (Issue Order):** order within a milestone

**Rules**
- **Same number = parallel OK.**  
- **Tie-breakers:** (1) **Priority** P0→P3, then (2) **smallest issue ID**.  
- **Dependencies override everything**: items `blocked` or with open blockers are **not eligible**.

**Example**
- Admin Portal milestones both **MO=2** → either can proceed; pick higher priority.  
- Inside `AP-Menu`, issues with **IO=1** can run in parallel; if only one dev, take the smaller ID first (after priority).

---

## 2) Create a Project (one per big deliverable)
1. Create an **organization-level Project**.  
2. Add fields:  
   - **Status:** Backlog → Ready → In Progress → In Review → QA → Done → Blocked  
   - **Priority:** P0–P3  
   - **Size:** XS–XL  
   - **Target Release:** text  
   - **Owner:** user  
   - **Risk:** Low/Medium/High  
   - *(Optional)* **Iteration**, **Critical Path (Yes/No)**  
   - **PO, MO, IO** *(Number)*
3. Create views: **Planning (Table)**, **Kanban (Board)**, **By Assignee (Table)**, **Roadmap (Timeline)**, **Blocked**, **Ready**, **Next Up**.  
4. Project description: goals, non-goals, links to design/ADRs.

---

## 3) Milestones (step-by-step)
1) Repo → **Issues** → **Milestones** → **New milestone**.  
2) **Title:** versioned or outcome-based.  
3) **Due date** set.  
4) **Description:**

```md
# Milestone: <name>
**Goal:** <what this delivers and why it matters>  
**In-scope:** <bullet list>  
**Out-of-scope:** <bullet list>  
**Acceptance criteria:**  
- [ ] <criterion 1>  
- [ ] <criterion 2>  
**Dependencies:** <issues/milestones/3rd parties>  
**Risks & mitigations:** <list>  
**Target Release (if outcome-based):** vX.Y  
**Owner:** @username
```
5) In the Project table, multi-select the milestone’s items and set **PO/MO** in bulk.  
6) Save a filtered Planning view for the milestone.

---

## 4) Issues (tasks)
For **each issue**:  
- Assign to **Project + Milestone + Owner**; set **Status**, **Priority**, **Size**.  
- Add **Dependencies** and **Subtasks**.  
- Set **IO** (Issue Order). Default `100`; lower means earlier.  
- Use closing keywords in PRs (`Closes #123`).

---

## 5) Dependencies — clear and visible
### 5.1 Linked Issues (UI)
Use **Link issues → Blocked by** / **Blocks** and add the `blocked` label when waiting on something.

### 5.2 Tasklist referencing issues (recommended)
```md
## Dependencies
- [ ] #123  <!-- API contract -->
- [ ] #124  <!-- DB migration merged -->
```
GitHub shows progress and links. When all are closed, remove `blocked` and set **Status → Ready**.

### 5.3 Project Views & Labels
Saved views **Blocked** (`label:blocked OR Status=Blocked`) and **Ready** (`label:ready-to-code AND Status=Ready`).  
**Cross-repo tip:** reference full URLs in tasklists (e.g., `- [ ] https://github.com/org/repo/issues/123`).

---

## 6) Subtasks — when & how
### 6.1 Checklist subtasks (no child issues) when:
- Each subtask **< 0.5 day**, **one assignee**, no separate QA/docs.
```md
## Subtasks
- [ ] Wire domain event
- [ ] Handle command validation
- [ ] Write unit tests
```

### 6.2 Child issues (heavyweight) when any applies:
- **> 0.5 day**, needs **parallelisation**, or multiple assignees.  
- Own **acceptance criteria**, testing, or **milestone**.  
- Involves **schema migration**, **security/privacy**, or **rollback** planning.
```md
## Subtasks (child issues)
- [ ] #321 FE component & storybook
- [ ] #322 API endpoint & tests
- [ ] #323 QA test plan & cases
- [ ] #324 Docs (admin + developer)
```

> Convert checklist item → **Convert to issue** from the task menu.

---

## 7) “Ready to Code” Gate
Add `ready-to-code` and set **Status → Ready** when all are true:
- Summary & background clear; **ACs testable**  
- UI/API/Data **contracts** linked (design/ADRs)  
- **Dependencies** listed and blockers resolved or covered by spikes  
- Size (XS–XL) and Priority (P0–P3) set  
- Repo(s), Milestone, Project, **Owner** set  
- Observability/test plan noted (logs/metrics/tests)  
- Security/privacy handled; rollout/feature flag plan if needed

**Automation idea:** `ready-to-code` → `Status=Ready`; `blocked` → `Status=Blocked`.

---

## 8) Weekly workflow
- **Triage:** Label, set Priority/Size/Owner, assign to Project/Milestone; add deps/subtasks; set IO.  
- **Planning:** Update **PO/MO/IO** to reflect sequencing & parallel chunks; confirm each issue has a milestone.  
- **Execute:** Move items Ready → In Progress → In Review → QA.  
- **Review & Merge:** PRs reference issues and pass checks.  
- **QA/Validation:** Use `qa-needed`; verify in Test/Quality envs.  
- **Release:** For Versioned milestones, tag release; for Outcome milestones, update **Target Release**.  
- **Retro:** Capture learnings; update templates/process.

---

## 9) Examples (ordering included)
### Versioned style
**Projects**
- `Core Backend — Architecture` *(PO=1)*
- `Admin Portal — Web` *(PO=2)*
- `Public API — External` *(PO=3)*

**Milestones (Core Backend)**
- `CB-Application v0.1` *(MO=1)*
- `CB-Domain v0.1` *(MO=2)*
- `CB-Infrastructure v0.1` *(MO=2)*  ← parallel with Domain

**Issues (AP-Menu v0.2)**
- `#101` Menu component scaffold *(IO=1, P1)*  
- `#102` Role-based menu resolver *(IO=1, P1)*  ← same order as #101; pick smaller ID first if one dev  
- `#103` Persist collapsed/expanded state *(IO=2, P2)*

### Outcome style
Milestone: `AP-Menu — Persist State` *(Target Release: v0.2, MO=2)*  
Issues inherit **PO=2**; set **IO** locally.

---

## 10) Views to create (with order)
- **Next Up (single-dev):** Filter `Status = Ready AND NOT label:blocked`. Sort by  
  **PO → MO → IO → Priority (P0..P3) → Issue Number (asc)**.  
- **Next Up (multi-dev):** same sort; select top *N* items (dependencies must be closed).  
- **By Milestone (ordered):** group by Milestone, sort **MO asc**, then **IO asc**.  
- **Roadmap:** Timeline grouped by Milestone; use due dates and **MO** for sequencing.

---

## 11) AI/Automation — selection algorithm (pseudocode)
```
INPUT: developer_count, items[] (status, labels, PO, MO, IO, priority, issue_id), deps
CANDIDATES = items where status == "Ready" AND NOT has_label("blocked") AND all_deps_closed(item)
PRIORITY_RANK(P0,P1,P2,P3) = (0,1,2,3)
SORT_KEY(item) = (item.PO, item.MO, item.IO, PRIORITY_RANK(item.priority), item.issue_id)
QUEUE = sort(CANDIDATES, by=SORT_KEY)
if developer_count == 1:
    return first(QUEUE)
else:
    return first_N(QUEUE, N=developer_count)
```

---

## 12) Automation (optional)
### 12.1 Project rules
- When `blocked` added → **Status=Blocked**.  
- When `ready-to-code` added → **Status=Ready**.

### 12.2 GitHub Actions
**Add to Project** (auto-add by `area:*` labels) — see marketplace action `actions/add-to-project`.  
**Dependency blocker** — toggles `blocked` based on tasklists:

Create **`.github/workflows/dependency-blocker.yml`**:
```yaml
name: Dependency blocker
on:
  issues:
    types: [opened, edited, reopened]
  schedule:
    - cron: "0 2 * * *"
permissions:
  issues: write
  contents: read

jobs:
  scan-dependencies:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/github-script@v7
        with:
          script: |
            const issue_number = context.payload.issue ? context.payload.issue.number : null;
            const repo = context.repo.repo;
            const owner = context.repo.owner;

            async function isBlockedByOpenDeps(number) {
              const { data: issue } = await github.rest.issues.get({ owner, repo, issue_number: number });
              const body = issue.body || "";
              const regex = /- \[ \] \#(\d+)/g; // unchecked tasks that reference issues
              let match, openDeps = 0;
              while ((match = regex.exec(body)) !== null) {
                const depNum = Number(match[1]);
                try {
                  const { data: dep } = await github.rest.issues.get({ owner, repo, issue_number: depNum });
                  if (dep.state !== "closed") openDeps++;
                } catch (e) {}
              }
              return openDeps > 0;
            }

            async function setBlockedLabel(number, blocked) {
              const { data: labels } = await github.rest.issues.listLabelsOnIssue({ owner, repo, issue_number: number });
              const hasBlocked = labels.some(l => l.name === "blocked");
              if (blocked && !hasBlocked) {
                await github.rest.issues.addLabels({ owner, repo, issue_number: number, labels: ["blocked"] });
              } else if (!blocked && hasBlocked) {
                await github.rest.issues.removeLabel({ owner, repo, issue_number: number, name: "blocked" }).catch(() => {});
              }
            }

            if (issue_number) {
              const blocked = await isBlockedByOpenDeps(issue_number);
              await setBlockedLabel(issue_number, blocked);
            } else {
              const { data: issues } = await github.rest.issues.listForRepo({ owner, repo, state: "open", per_page: 200 });
              for (const it of issues) {
                const blocked = await isBlockedByOpenDeps(it.number);
                await setBlockedLabel(it.number, blocked);
              }
            }
```

---

## 13) Issue templates (paste into `.github/ISSUE_TEMPLATE/`)
### Feature
```md
---
name: Feature
about: Implement a new capability
labels: ["type:feature"]
---

## Summary
<one-liner>

## Background / Why
<business/technical context>

## Acceptance Criteria
- [ ] AC1
- [ ] AC2

## Dependencies
- [ ] <link or #id>
- [ ] <link or #id>

## Subtasks
- [ ] <subtask or #child-issue>
- [ ] <subtask or #child-issue>

## Definition of Ready
- [ ] Acceptance criteria testable
- [ ] Dependencies resolved or spiked
- [ ] Size estimated (XS–XL), Priority set
- [ ] Milestone & Project assigned, Owner set
- [ ] Observability/test plan noted
- [ ] Security/privacy reviewed if relevant

## Definition of Done
- [ ] All ACs met
- [ ] Tests updated/green
- [ ] Docs updated (user/admin/dev)
- [ ] Security/QA checks passed

## Links
Design: <URL>
ADRs: <URL>
Related: #123 #456
```

### Tech Task
```md
---
name: Tech Task
about: Internal engineering work
labels: ["type:tech-debt"]
---

## Objective
<what we are changing>

## Approach
<how we’ll do it; options if needed>

## Dependencies
- [ ] <link or #id>

## Risks
<perf, security, migration>

## Subtasks
- [ ] <subtask or #child-issue>

## DoD
- [ ] Unit/integration tests
- [ ] Migration/backwards compatibility considered
- [ ] Observability in place (logs/metrics)
```

### Bug
```md
---
name: Bug
about: Something is broken
labels: ["type:bug"]
---

## Observed
<what happens>

## Expected
<what should happen>

## Steps to Reproduce
1.
2.

## Environment
<version / env>

## Severity
P0 | P1 | P2 | P3

## Dependencies
- [ ] <link or #id>

## Attachments/Logs
```

### Spike
```md
---
name: Spike
about: Time-boxed investigation
labels: ["type:spike"]
---

## Question
<what we need to learn/decide>

## Timebox
<e.g., 1 day>

## Deliverables
- [ ] Short write-up of findings
- [ ] Recommended next steps

## Dependencies
- [ ] <link or #id>
```
---

## 14) FAQ (for juniors)
**Q: Milestone vs Sprint?** Milestone = deliverable (outcome or version). Sprint = time-box.  
**Q: Where do I put the version for outcome-based milestones?** In **Target Release**.  
**Q: Who marks “Ready to Code”?** Owner or Tech Lead after the Ready checklist.  
**Q: What if I’m blocked?** Link blocker under **Dependencies**, add `blocked`, it drops from **Next Up**.  
**Q: How do I pick my next task?** Open **Next Up**, take the first eligible card (PO→MO→IO→Priority→ID; deps closed).

---

## 15) Kick-off Checklist
- [ ] Project created at org level  
- [ ] Fields added (Status, Priority, Size, Target Release, Owner, Risk, Iteration, Critical Path, **PO/MO/IO**)  
- [ ] Views saved (Planning, Kanban, By Assignee, Roadmap, Blocked, Ready, **Next Up**)  
- [ ] Milestones created with names per **Versioned** or **Outcome** style  
- [ ] Issue templates committed (Feature, Tech Task, Bug, Spike)  
- [ ] Labels created (type/area/priority/size/flags incl. `blocked`, `ready-to-code`)  
- [ ] Actions added (`add-to-project`, `dependency-blocker`)  
- [ ] DoR/DoD/Ready Gate confirmed with team  
- [ ] Project README links to design/ADRs/test plan
