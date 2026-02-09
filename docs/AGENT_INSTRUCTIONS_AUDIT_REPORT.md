# Agent Instructions Audit Report

**Date:** 2026-02-04  
**Scope:** `.cursor/rules/*`, `.agent/rules/*` (Antigravity), `.github/copilot-instructions.md`, `AGENTS.md`  
**Reference Benchmarks:** [Antigravity](https://antigravity.google/docs/rules-workflows), [Cursor Rules](https://cursor.com/docs/context/rules), [GitHub Copilot](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions)

---

## 1. Alignment Scores

| Benchmark | Score | Rationale |
|-----------|-------|-----------|
| **Antigravity (Rules/Workflows)** | 6/10 | Good: Rules in Markdown, activation modes. Gaps: No explicit Trigger→State→Action flow; no Workflows (slash commands); Rules lack imperative "When X, do Y" structure. |
| **Cursor Rules** | 7/10 | Good: Modular .mdc files, frontmatter (description, alwaysApply), focused content. Gaps: project-rules.mdc duplicates 00–60 (~500 lines wasted); no @file references; no globs for file-scoped rules; Skills list doesn't reference Cursor Skills (`@create-rule`, etc.). |
| **GitHub Copilot** | 7/10 | Good: copilot-instructions.md has build/test/run, guardrails (NEVER), project layout. Gaps: No path-specific `.github/instructions/*.instructions.md`; build section could follow GitHub's structured format (bootstrap, build, test, run, lint); test path inconsistency. |

---

## 2. Critical Fixes

### 2.1 Domain Error: Orders vs Sales (HIGH)

**Location:** `00-context-architecture.mdc` (line 35), `project-rules.mdc` (line 35)

**Current (WRONG):**
```text
4. **Orders Service** - Sales orders, order processing
```

**Correct:**
```text
4. **Orders Service** - Operational logistics (transfers, inbound/outbound movements). NOT commercial sales.
5. **Sales Service** - Commercial sales (SalesOrders, customers, quotes, pricing).
```

**Impact:** Agents may add `CustomerId` or `TotalAmount` to Orders domain, violating DDD bounded context. The bounded context section in copilot-instructions and `.agent/rules/RULES.md` is correct; 00-context and project-rules contradict it.

---

### 2.2 Massive Redundancy: project-rules.mdc (HIGH)

**Problem:** `project-rules.mdc` (~500 lines) duplicates content from `00-context-architecture.mdc`, `10-code-generation.mdc`, `20-service-communication.mdc`, `30-security-testing.mdc`, `40-workflows-and-conventions.mdc`, `50-anti-patterns.mdc`, `60-skills-list.mdc`. All have `alwaysApply: true`, so the agent receives the same guidance twice.

**Fix:** Replace `project-rules.mdc` with a thin orchestrator that @-references the modular rules, or remove it and rely on the numbered rules.

---

### 2.3 Test Path Inconsistency (MEDIUM)

**copilot-instructions.md** (line 33): `Tests: src/AppHost.Tests/Tests/`  
**Reality:** Unit tests live in `src/MyApp.[Service]/test/MyApp.[Service].Application.Tests/` and `MyApp.[Service].Infrastructure.Tests/`. AppHost.Tests is for integration tests.

**Fix:** Document both:
- Unit tests: `src/MyApp.[Service]/test/`
- Integration tests: `src/AppHost.Tests/`

---

### 2.4 DAPR vs Dapr Casing (LOW)

**Inconsistency:** "DAPR" (all caps) vs "Dapr" (official branding).  
**Fix:** Use "Dapr" consistently (official name).

---

### 2.5 Skills List vs Cursor Skills (MEDIUM)

**Current:** `60-skills-list.mdc` lists project stack skills (e.g., .NET 10, Dapr).  
**Missing:** No mention of when to use Cursor Agent Skills: `@create-rule`, `@create-skill`, `@create-subagent`, `@migrate-to-skills`, `@update-cursor-settings`.

**Fix:** Add a short section: "When the user asks to create a rule, skill, or migrate rules, use the corresponding Agent Skill via @mention."

---

## 3. Refined Instructions (Gold Standard)

### 3.1 Refactored project-rules.mdc (Thin Orchestrator)

Replace the 500-line duplicate with:

```markdown
---
description: "ERP Microservices - orchestrator; references modular rules"
alwaysApply: true
---

# ERP Microservices - Rule Index

This project uses modular rules. When working in this codebase, the following rules apply:

- **Architecture & Context:** @00-context-architecture.mdc
- **Code Generation:** @10-code-generation.mdc
- **Service Communication:** @20-service-communication.mdc
- **Security & Testing:** @30-security-testing.mdc
- **Workflows:** @40-workflows-and-conventions.mdc
- **Anti-Patterns:** @50-anti-patterns.mdc
- **Skills:** @60-skills-list.mdc

## Cursor Agent Skills

When the user asks to create a rule, skill, subagent, or migrate rules, use the corresponding Agent Skill:
- Create rule → @create-rule
- Create skill → @create-skill
- Migrate to skills → @migrate-to-skills
- Update settings → @update-cursor-settings
```

---

### 3.2 Corrected 00-context-architecture.mdc (Microservices List)

```markdown
### Microservices

1. **Auth Service** - Authentication, authorization, user management
2. **Billing Service** - Invoicing, payments, billing
3. **Inventory Service** - Products, warehouses, stock management
4. **Orders Service** - Operational logistics (transfers, inbound/outbound movements). Links to Sales/Purchasing via ExternalOrderId. NEVER contains CustomerId, SupplierId, or pricing.
5. **Purchasing Service** - Purchase orders, supplier management
6. **Sales Service** - Sales operations, SalesOrders, customers, quotes, pricing
```

---

### 3.3 Imperative Additions (Antigravity-Style Trigger→Action)

Add to `10-code-generation.mdc`:

```markdown
## Trigger-Based Actions

**When adding a new API endpoint:**
1. Create DTO in `[Service].Application.Contracts/DTOs/`
2. Define `I[Entity]Repository` in `[Service].Domain/Repositories/`
3. Implement repository in `[Service].Infrastructure/Data/Repositories/`
4. Create `I[Entity]Service` and implement in `[Service].Application/Services/`
5. Create controller in `[Service].API/Controllers/`
6. Register in `Program.cs`
7. Update `src/ErpApiGateway/ocelot.json` if new controller

**When modifying domain entities in Orders:**
- If you need CustomerId, SupplierId, TotalAmount, or UnitPrice → STOP. That belongs in Sales or Purchasing.
```

---

### 3.4 GitHub copilot-instructions.md – Build Section (Structured)

Add a clearly structured build section per GitHub's recommended format:

```markdown
## Build & Validation

### Bootstrap
- `dotnet restore` (or implicit via build)

### Build
- `dotnet build` (Debug)
- `dotnet build -c Release` (Release)

### Test
- `dotnet test` (all tests)
- Unit tests: `src/MyApp.[Service]/test/`
- Integration tests: `src/AppHost.Tests/`

### Run
- Full stack: `cd src/AppHost && dotnet run`
- Single service: `cd src/MyApp.[Service]/MyApp.[Service].API && dotnet run`

### Lint
- (Add if you have a linter, e.g., dotnet format, StyleCop)

### Pre-commit
- `dotnet build && dotnet test` must pass
```

---

### 3.5 Guardrails Consolidation (GitHub-Style Negative Constraints)

Ensure these "NEVER" rules appear in a single, authoritative place (e.g., `50-anti-patterns.mdc`):

```markdown
## NEVER Do

1. Add CustomerId, SupplierId, TotalAmount, or UnitPrice to Orders domain
2. Access another service's database directly
3. Return domain entities from API controllers (use DTOs)
4. Hardcode secrets or credentials
5. Use .Result or .Wait() on async methods
6. Log passwords, tokens, or PII
7. Use string concatenation for SQL (use parameterized queries)
8. Put business logic in controllers
9. Share DbContext across service boundaries
10. Skip DTOs for API contracts
```

---

## 4. Modularization Recommendations

| File | Action |
|------|--------|
| `project-rules.mdc` | Replace with thin orchestrator (~30 lines) that @-references 00–60 |
| `copilot-instructions.md` | Consider splitting: `.github/instructions/csharp.instructions.md` (applyTo: `**/*.cs`), `.github/instructions/bicep.instructions.md` (applyTo: `infra/**/*.bicep`) |
| `.agent/rules/RULES.md` | Antigravity workspace rules (correct location per [Antigravity docs](https://antigravity.google/docs/rules-workflows)). Standard naming matches global rules pattern (`GEMINI.md`). Keep in sync with Cursor rules. Consider as SSOT for DDD boundaries. |

---

## 5. Summary Checklist

- [ ] Fix Orders vs Sales description in `00-context-architecture.mdc` and `project-rules.mdc`
- [ ] Replace `project-rules.mdc` with thin orchestrator
- [ ] Add Cursor Agent Skills reference to rules
- [ ] Fix test path documentation in copilot-instructions
- [ ] Standardize "Dapr" casing
- [ ] Add Trigger→Action imperative flows to code-generation rule
- [ ] Add structured build section to copilot-instructions
- [ ] Ensure guardrails are consolidated and non-contradictory

---

**Questions for You**

1. **Antigravity vs Cursor:** Do you use both IDEs? If so, should `.agent/rules/RULES.md` be the canonical DDD/bounded-context source, with Cursor rules @-referencing it?
2. **project-rules.mdc:** Prefer removal (rely on 00–60) or thin orchestrator with @-references?
3. **Path-specific instructions:** Do you want `.github/instructions/*.instructions.md` for C# vs Bicep vs YAML, or is a single copilot-instructions.md sufficient?
