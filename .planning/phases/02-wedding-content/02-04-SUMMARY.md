---
plan: 02-04
phase: 02-wedding-content
status: complete
commit: 0b641e4
---

# Plan 02-04 Summary — AlloggioPopup

## What Was Built

Created `AlloggioPopup.cs` at `Assets/App/Scripts/Popup/` — a `Popup` subclass that displays accommodation info branched on the current group's data.

**OnShowing behavior:**
- Guard: if `App.Instance == null` or `CurrentGroup == null` → show general panel (safe fallback)
- `hasApartment = true` → show `apartmentPanel`, populate name/address/notes labels from `CurrentGroup`
- `hasApartment = false` → show `generalSuggestionsPanel`

Notes are displayed as `string.Join("\n", group.apartmentNotes)` — one entry per line, no prefab instantiation.

## Key Files

### Created
- `Assets/App/Scripts/Popup/AlloggioPopup.cs` — 69 lines

## Verification

- `AlloggioPopup : Popup` → line 4 ✓
- `base.OnShowing(inData)` → line 29 ✓
- `group.hasApartment` branch → line 39 ✓
- `string.Join("\n", ...)` for notes → line 50 ✓
- Commit: `0b641e4`

## Decisions

| Decision | Rationale |
|----------|-----------|
| `base.OnShowing(inData)` called first | Respects Popup base class contract; base may hook animation timing |
| `SetPanelActive` private helper | Keeps `OnShowing` readable; ensures both panels are always set consistently (avoids one panel being shown when the other should be hidden) |
| All labels null-checked individually | Developer may leave unused labels unassigned in Inspector; null check prevents Inspector-setup mistakes from crashing the popup |
| `string.Join("\n", group.apartmentNotes)` | Simplest approach for 2-3 notes — no prefab instantiation, no coroutines, no scroll view required at this scale |

## Self-Check: PASSED
