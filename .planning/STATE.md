---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 04
status: executing
last_updated: "2026-04-20T08:40:14.822Z"
progress:
  total_phases: 4
  completed_phases: 3
  total_plans: 11
  completed_plans: 8
  percent: 73
---

# Project State

**Project:** Matrimiorci
**Milestone:** v1.0 — Wedding Launch
**Current Phase:** 04
**Status:** Executing Phase 04

---

## Phase Status

| # | Phase | Status | Plans |
|---|-------|--------|-------|
| 1 | Foundation | ✅ Complete | 2 plans — commits 66a4688, 16b29c1, 1f48b96, 33aeeaf |
| 2 | Wedding Content | ✅ Complete | 4 plans — commits f92ea7c, bb9b4f9, 51f405b, 0b641e4 |
| 3 | RSVP | ✅ Complete | 2 plans — commits a15c70e, 1188913, 5551fd3 |
| 4 | WebGL Build & QA | Not started | TBD |

---

## Last Action

Phase 03 RSVP executed — April 17, 2026

- GuestRowUI.cs created (per-guest row component: name pre-fill, attendance toggles, meal ToggleGroup, public accessors)
- RSVPPopup.cs created (Popup subclass: OnShowing populates rows, SubmitRSVP coroutine, PlayerPrefs submitted flag, breakfast panel gating)
- RSVPSheet.gs created (Google Apps Script doGet handler, flat indexed params, formula injection sanitization)

---

## Next Action

Run `/gsd-plan-phase 4` to plan Phase 04 (WebGL Build & QA)

---

## Key Decisions (Accumulated)

| Decision | Rationale |
|----------|-----------|
| Use Gzip (not Brotli) for WebGL build | GitHub Pages does not serve custom Content-Encoding headers; Gzip is reliable without a custom server |
| Google Apps Script GET (not POST) | POST bodies are silently dropped on the 302 redirect Apps Script always returns |
| Hardcode all passwords in Inspector List | <10 groups, 2-week deadline — no CMS needed |
| PlayerPrefs.Save() immediately on every write | `OnApplicationQuit` does not fire on browser tab close in WebGL |
| ContentGate subscribes to App.OnUnlocked event | Decoupled, no central GateManager needed; each gate handles its own state |
| Lock overlay is a sibling of content root | SetActive(false) on content must not hide the overlay |
| SectionButton: RemoveAllListeners before AddListener | OnUnlocked fires multiple times per session; listener accumulation must be prevented explicitly |
| visibleToPasswords empty = show all authenticated | Developer opt-in restriction model; most content is per-authenticated-group, not per-individual |
| RSVPPopup: flat indexed GET params | guest0Name/guest0Attendance/guest0Meal pattern; most debuggable, safe for ≤10 guests under 2048-char Apps Script URL limit |
| Uri.EscapeDataString() on all URL params | WWW.EscapeURL() removed in Unity 2022+; Italian names/notes must be encoded to avoid silent corruption in Apps Script e.parameter |
| RSVPSubmitted PlayerPrefs key (int) | Per-device flag prevents re-submission; PlayerPrefs.Save() called immediately on success |
| Apps Script access must be "Anyone" | "Anyone with link" or authenticated causes 401/redirect; Unity receives no 200 ok response |

---

## Blockers

None

---

## Session Continuity

- **Roadmap:** `.planning/ROADMAP.md`
- **Requirements:** `.planning/REQUIREMENTS.md`
- **Research:** `.planning/research/SUMMARY.md`, `.planning/research/ARCHITECTURE.md`
- **Codebase:** `.planning/codebase/ARCHITECTURE.md`, `.planning/codebase/STACK.md`

---

*Last updated: April 17, 2026 — Phase 01 Foundation complete*
