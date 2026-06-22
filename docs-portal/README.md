# NumbatWallet Documentation Portal

A single static, **audience-filtered** documentation site (docs-as-code) for the
NumbatWallet platform. Built with **[MkDocs Material](https://squidfunk.github.io/mkdocs-material/)**.

The portal presents five audience lenses over shared, tag-filtered content:

| Audience | What they get |
|---|---|
| **Directors** | Status & readiness, SLA verdict, compliance posture (claims, not certifications), risk register |
| **Account Management** | Capability matrix (live / stub / roadmap), per-SDK pilot readiness |
| **Support** | Environments & URLs, runbook, known issues, open operator follow-ups |
| **Tech** | As-built architecture, auth schemes, API surface (REST + GraphQL), data protection, deployment & CI |
| **Clients** | Getting started, auth model, the three SDK quickstarts with honest coverage notes |

## Layout

```
mkdocs.yml                      # site config (theme, nav, tags plugin); docs_dir = docs-portal/content
docs-portal/
├── README.md                   # this file
├── content/                    # all markdown pages (front-matter `tags:` per audience)
│   ├── index.md                # landing page + audience chooser
│   ├── tags.md                 # tag index (auto-populated by the tags plugin)
│   ├── stylesheets/extra.css   # status-icon colours
│   ├── directors/  account-management/  support/  tech/  clients/
└── site/                       # built static output (gitignored — do not commit)
```

Content is **sourced from** the repository's as-built docs (`README.md`,
`docs/ARCHITECTURE-CURRENT.md`, `docs/OPERATIONS.md`, `perf/RESULTS-2026-06-12.md`) and the
SDK READMEs — it summarises and links them rather than duplicating-and-drifting.

## Prerequisites

Python 3 with MkDocs Material. A virtualenv is the cleanest install (the repo root already
has a `.docs-venv/` used to build this; it is gitignored):

```bash
python3 -m venv .docs-venv
.docs-venv/bin/pip install mkdocs-material
```

## Preview (live reload)

```bash
.docs-venv/bin/mkdocs serve
# → http://127.0.0.1:8000
```

## Build (static output)

```bash
.docs-venv/bin/mkdocs build --strict
# → static site written to docs-portal/site/ (gitignored)
```

`--strict` fails the build on broken internal links or nav references — keep it green.

## Audience filtering

Two mechanisms work together:

1. **Top-level nav** — one section per audience; reading a section top-to-bottom is a
   coherent view for that reader.
2. **Tags** — every page declares `tags: [...]` in front-matter; the **Tags** page lists
   every page grouped by audience, so cross-cutting pages (e.g. the risk register is both
   *directors* and *support*) surface in each relevant audience's tag list.
