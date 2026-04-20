---
phase: 02
status: clean
depth: standard
reviewed_files: 4
findings: 0
reviewed_at: "2026-04-17"
---

# Phase 02 Code Review

## Scope

Files changed in Phase 02 — Wedding Content:

1. `Assets/App/Scripts/Managers/GroupData.cs` (modified)
2. `Assets/App/Scripts/Utils/GroupContentGate.cs` (created)
3. `Assets/App/Scripts/UI/SectionButton.cs` (created)
4. `Assets/App/Scripts/Popup/AlloggioPopup.cs` (created)

## Findings

**No issues found.** All four files pass standard review.

## File-by-File Notes

### GroupData.cs
- New fields correctly appended to end of `#region Inspector Variables` — preserves Unity serialization order for existing scene data
- All fields have `[Tooltip]` attributes per project convention
- `List<string> apartmentNotes` initialized with `new List<string>()` — avoids null reference in `string.Join`

### GroupContentGate.cs
- Null guard chain in `Refresh()` is correct and complete: `content` → `App.Instance` → `IsUnlocked` → empty list → password match
- `CurrentGroup?.password ?? string.Empty` null-coalesces correctly; `CurrentGroup` should never be null when `IsUnlocked = true` but the defensive check is appropriate
- `using System;` import correctly included for `StringComparison.OrdinalIgnoreCase`

### SectionButton.cs
- `RemoveAllListeners()` before every `AddListener()` in `Refresh()` — correctly prevents listener accumulation across multiple `OnUnlocked` events
- `_button` cached in `Awake()` — not re-fetched per `Refresh()` call
- `if (_button == null) return;` guard handles missing Button component gracefully
- `string.IsNullOrEmpty(popupId)` guard in `OpenTargetPopup()` prevents crash on misconfigured Inspector field

### AlloggioPopup.cs
- `base.OnShowing(inData)` called before any branching — respects Popup base class contract
- Double null guard (`App.Instance == null || CurrentGroup == null`) safely falls back to general panel
- `SetPanelActive` helper keeps `OnShowing` readable and ensures both panels are always set in tandem
- All label fields individually null-checked — tolerates partial Inspector wiring

## Security Assessment

- No user-controlled input enters any of the new components
- `visibleToPasswords` is developer-set at design time (Inspector), not guest-inputted
- Password comparison uses `OrdinalIgnoreCase` — no locale-sensitive comparison pitfalls
- No serialization of sensitive data to disk in this phase

## Summary

Phase 02 code is idiomatic Unity C#, follows all project conventions (`#region` blocks, `[Tooltip]`, `[SerializeField]` private fields, `SingletonComponent` access via `App.Instance`), and implements correct Unity MonoBehaviour lifecycle patterns.
