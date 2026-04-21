---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 04
status: complete
last_updated: "2026-04-21T00:00:00.000Z"
progress:
  total_phases: 4
  completed_phases: 4
  total_plans: 11
  completed_plans: 11
  percent: 100
---

# Project State

**Project:** Matrimiorci
**Milestone:** v1.0 — Wedding Launch
**Current Phase:** 04
**Status:** ✅ Milestone Complete

---

## Phase Status

| # | Phase | Status | Plans |
|---|-------|--------|-------|
| 1 | Foundation | ✅ Complete | 2 plans — commits 66a4688, 16b29c1, 1f48b96, 33aeeaf |
| 2 | Wedding Content | ✅ Complete | 4 plans — commits f92ea7c, bb9b4f9, 51f405b, 0b641e4 |
| 3 | RSVP | ✅ Complete | 2 plans — commits a15c70e, 1188913, 5551fd3 |
| 4 | WebGL Build & QA | ✅ Complete | 3 plans — Player Settings, GitHub Pages deploy, QA sign-off |

---

## Last Action

Phase 04 WebGL Build & QA executed — April 21, 2026

- SetAudioCompression.cs Editor tool created (batch-sets all AudioClips to CompressedInMemory)
- DOTween link.xml created (prevents managed code stripping)
- Unity Player Settings applied (WebAssembly 2023, 32MB heap, Disabled compression)
- WebGL build compiled and deployed to GitHub Pages (`gh-pages` branch)
- **Gzip → Disabled compression switch:** GitHub Pages CDN double-decompression bug (`framework.js.gz` served with Content-Encoding:gzip + Unity re-decompresses → Syntax Error). Fixed by switching to Disabled compression.
- Manual QA passed: Windows (Chrome/Firefox/Edge), Android (Chrome), iOS (Safari)

---

## Next Action

✅ **v1.0 milestone complete.** The wedding app is live on GitHub Pages and QA-verified across all target platforms.

---

## Key Decisions (Accumulated)

| Decision | Rationale |
|----------|-----------|
| Use Disabled compression for WebGL build | **Changed from Gzip during Phase 04:** GitHub Pages Fastly CDN serves `.framework.js.gz` with Content-Encoding:gzip (double-decompression → Syntax Error at 1:1). Disabled compression avoids all CDN interference. Size larger (~40-50MB) but acceptable for this scale. |
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
