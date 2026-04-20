---
plan: 02-02
phase: 02-wedding-content
status: complete
commit: bb9b4f9
---

# Plan 02-02 Summary — GroupContentGate

## What Was Built

Created `GroupContentGate.cs` at `Assets/App/Scripts/Utils/` — a MonoBehaviour that shows or hides a content `GameObject` based on:

1. Whether any group is unlocked (`App.Instance.IsUnlocked`)
2. Optionally, whether the unlocked group's password is in `visibleToPasswords` list

**Visibility logic (Refresh guard chain):**
- `content == null` → return (safe no-op)
- `App.Instance == null` → hide (editor/test guard)
- `!IsUnlocked` → hide
- `visibleToPasswords` empty → show (all authenticated groups)
- else → show only if `CurrentGroup.password` matches any entry (OrdinalIgnoreCase)

Subscribes to `App.OnUnlocked` in `OnEnable`, unsubscribes in `OnDisable` — identical pattern to `ContentGate.cs`.

## Key Files

### Created
- `Assets/App/Scripts/Utils/GroupContentGate.cs` — 58 lines

## Verification

- `GroupContentGate` class declaration → line 5 ✓
- `App.OnUnlocked += Refresh` → line 21 ✓
- `App.OnUnlocked -= Refresh` → line 27 ✓
- `OrdinalIgnoreCase` comparison → line 48 ✓
- Commit: `bb9b4f9`

## Decisions

| Decision | Rationale |
|----------|-----------|
| Separate component from ContentGate | ContentGate handles binary lock/unlock; GroupContentGate adds per-group password filter on top — single responsibility |
| `visibleToPasswords` empty = show all | Developer opt-in model: leave empty for "all guests", populate for specific groups only |
| `?.password ?? string.Empty` null-coalescence | CurrentGroup should never be null when IsUnlocked=true, but defensive null-coalesce prevents crash in edge cases |

## Self-Check: PASSED
