# Phase 4: WebGL Build & QA - Context

**Gathered:** April 20, 2026
**Status:** Ready for planning

<domain>
## Phase Boundary

Produce a production-ready Unity 6 WebGL build and deploy it to GitHub Pages at HTTPS. The build must pass size, memory, and audio requirements (TECH-01 through TECH-08) and be manually verified working on Windows browsers, a physical Android device, and a physical iOS device.

</domain>

<decisions>
## Implementation Decisions

### Compression
- **D-01:** Use **Gzip** compression — NOT Brotli. GitHub Pages does not serve custom `Content-Encoding` headers; Brotli would require a server-side workaround. Gzip works out-of-the-box.
  - Note: REQUIREMENTS.md TECH-01 says "Brotli" — this is superseded. Gzip is the correct choice for GitHub Pages. Planner must use Gzip.

### Hosting
- **D-02:** Deploy to **GitHub Pages** from this repository (same repo). No separate deployment repo.
- **D-03:** HTTPS is provided automatically by GitHub Pages — no extra config needed for that requirement.
- **D-04:** Exact GitHub Pages source branch and Pages configuration is **agent's discretion** (typically `gh-pages` branch or `/docs` folder on `main`).

### QR Code / URL Routing
- **D-05:** Physical invite QR code points to `?type=invite` — loads the Invite scene first.
- **D-06:** Default URL (no param) loads the Home scene directly (consistent with ROUT-03).
- **D-07:** No other URL params are needed at this time.

### Splash Screen
- **D-08:** Unity logo is already disabled in the Editor. The developer handles the custom logo / full splash removal manually — **do not touch splash screen settings in plans**.

### Device Testing
- **D-09:** Testing is done manually by the developer on:
  - Windows — multiple browsers (Chrome, Edge, Firefox at minimum)
  - Android — physical device
  - iOS — physical device (iPhone)
  - No simulators or BrowserStack

### the agent's Discretion
- GitHub Pages source configuration (branch: `gh-pages` vs `/docs` on main) — planner picks the simpler approach
- Whether to include a `.nojekyll` file (needed to prevent Jekyll from stripping Unity's underscored files)
- Build output folder naming

</decisions>

<specifics>
## Specific Ideas

- The QR code URL must use `?type=invite` — this is what will be printed on physical invitations. Getting this right before printing is critical.
- TECH-03: Initial memory heap = 32MB (not Unity default 256MB) — this is a hard requirement for mobile browser compatibility.
- TECH-05: All AudioClips must use CompressedInMemory — required for iOS Silent Mode compatibility (standard Unity audio API fails silently on iOS when device is muted unless this is set).
- TECH-04: WebAssembly 2023 + BigInt must be enabled — Unity 6 default, but must be verified.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements are fully captured in decisions above and in `.planning/REQUIREMENTS.md` (TECH-01 through TECH-08).

</canonical_refs>

<code_context>
## Existing Code & Settings Insights

### Current ProjectSettings State
- `m_ShowUnitySplashScreen: 1` in `ProjectSettings/ProjectSettings.asset` — splash is still enabled in the file (Unity logo reportedly disabled in Editor UI; planner should not modify splash settings)
- WebGL build target present (`m_BuildTarget: WebGLSupport`) but no WebGL-specific player settings overrides detected yet
- `Library/BuildProfiles/` contains platform profiles — no WebGL-specific build profile configured
- `m_CompressionType: -1` in SharedProfile.asset — compression not yet set (must be set to Gzip in plans)

### SafeArea
- `SafeArea.cs` is already implemented (Phase 1) — TECH-06 is complete, no work needed.

### App.cs
- `Application.targetFrameRate = 120`, `QualitySettings.vSyncCount = 0`, `SleepTimeout.NeverSleep` already set — these are fine for WebGL.

</code_context>

<deferred>
## Deferred Ideas

None surfaced during discussion.

</deferred>

---

*Phase: 04-webgl-build-qa*
*Context gathered: April 20, 2026*
