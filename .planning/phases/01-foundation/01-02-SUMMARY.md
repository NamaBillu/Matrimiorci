---
phase: 01-foundation
plan: 02
subsystem: auth
tags: [popup, password, content-gate, unity-ui, tmp, webgl]

requires:
  - phase: 01-foundation
    plan: 01
    provides:
      - App.TryUnlock(password) returning bool
      - App.OnUnlocked static event
      - App.IsUnlocked property

provides:
  - PasswordPopup.cs — Popup subclass guests use to enter password
  - ContentGate.cs — MonoBehaviour that shows/hides content based on unlock state
  - GateMode enum (LockedVisible / Hidden) — configurable per component in Inspector

affects:
  - Phase 2 (ContentGate components gate personalized sections)
  - Phase 3 (ContentGate gates RSVP form)

tech-stack:
  added:
    - TMPro (TMP_InputField, TextMeshProUGUI) — already in project, now used in Popup subclass
  patterns:
    - Popup subclass overrides OnShowing(object[] inData) to reset UI state
    - ContentGate uses OnEnable/OnDisable to subscribe/unsubscribe from static events — leak-safe
    - Refresh() called in OnEnable with null-check on App.Instance — handles component activated before App initialized
    - Tooltip documents sibling constraint (lockOverlay must not be child of content)

key-files:
  created:
    - Assets/App/Scripts/Popup/PasswordPopup.cs
    - Assets/App/Scripts/Utils/ContentGate.cs

key-decisions:
  - "GateMode enum declared at file scope (not nested) — accessible by any script needing to check mode externally"
  - "OnShowing clears input AND hides error — both reset needed for reopened popup UX"
  - "Refresh() called in OnEnable even if OnUnlocked hasn't fired — handles components added to already-unlocked screen"
  - "null-check App.Instance in Refresh() — component may be instantiated before Bootloader completes in Editor"

patterns-established:
  - "ContentGate pattern: OnEnable += event, Refresh(); OnDisable -= event; Refresh reads IsUnlocked not event param"
  - "Popup unlock flow: OnSubmit → TryUnlock → true? Hide(false) : ShowError(italian message)"

requirements-completed:
  - PASS-01
  - PASS-04
  - GATE-01
  - GATE-02
  - GATE-03

duration: 8min
completed: 2026-04-17
---

# Phase 01, Plan 02: PasswordPopup + ContentGate

**Password entry UI and content gating wired to the unlock system. Guests can now type a code, receive Italian error feedback on failure, and have content gates respond immediately on success.**

## Performance

- **Duration:** ~8 min
- **Completed:** 2026-04-17
- **Tasks:** 2
- **Files created:** 2

## Accomplishments

### PasswordPopup.cs — Password entry popup

`Popup` subclass at `Assets/App/Scripts/Popup/PasswordPopup.cs`.

- `OnShowing(object[] inData)` — clears input field text and hides error label every time the popup opens (preventing stale state when reopened)
- `OnSubmit()` — called by the Submit button's `OnClick` event; trims nothing (trimming is done inside `App.TryUnlock`); on success calls `Hide(false)` (popup slides out, cancelled=false); on failure shows `"Codice non valido. Riprova."` in red label
- `[SerializeField]` fields: `TMP_InputField codeInputField` and `TextMeshProUGUI errorLabel` — wired via Inspector

### ContentGate.cs — Content gating component

Utility `MonoBehaviour` at `Assets/App/Scripts/Utils/ContentGate.cs`. Attach to any UI container needing to be gated.

- **`GateMode.LockedVisible`** (default) — content stays visible but a semi-transparent overlay `lockOverlay` sits on top; on unlock the overlay is hidden. The overlay must be a **sibling** (not child) of `content` — documented in `[Tooltip]`.
- **`GateMode.Hidden`** — the `content` GameObject is toggled inactive until unlocked.
- Subscribes to `App.OnUnlocked` in `OnEnable`, unsubscribes in `OnDisable` — no memory leak.
- `Refresh()` called on `OnEnable` with `App.Instance != null` null-check — handles components that appear on screen after unlock has already fired (e.g., user opens Home scene content that was already unlocked in the same session).

## Commits

- `1f48b96` — feat(01-02): create PasswordPopup with TryUnlock integration
- `33aeeaf` — feat(01-02): create ContentGate with OnUnlocked subscription

## Verification

- `PasswordPopup.cs` has `App.Instance.TryUnlock(code)` ✓
- `PasswordPopup.cs` has `Hide(false)` on success ✓
- `PasswordPopup.cs` has `OnShowing(object[] inData)` override ✓
- `PasswordPopup.cs` has `"Codice non valido. Riprova."` ✓
- `ContentGate.cs` has `App.OnUnlocked += Refresh` and `App.OnUnlocked -= Refresh` ✓
- `ContentGate.cs` has `GateMode.LockedVisible` and `GateMode.Hidden` ✓
- `ContentGate.cs` has sibling constraint documented in Tooltip ✓
- `ContentGate.cs` has `App.Instance != null` null-check ✓

## Notes for downstream phases

- To gate a UI panel with overlay: Add `ContentGate` → set `mode = LockedVisible` → wire `lockOverlay` sibling → wire `content` parent
- To fully hide content: Add `ContentGate` → set `mode = Hidden` → wire `content`
- `PasswordPopup` is shown via `PopupManager.Instance.Show("PasswordPopup")` — the prefab must be registered in PopupManager's inspector list with id `"PasswordPopup"`
