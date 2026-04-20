---
plan: 02-03
phase: 02-wedding-content
status: complete
commit: 51f405b
---

# Plan 02-03 Summary — SectionButton

## What Was Built

Created `SectionButton.cs` at `Assets/App/Scripts/UI/` (new directory, auto-created by Unity). A MonoBehaviour that wraps any UI `Button` with two-state click behavior:

- **Locked:** click opens `PasswordPopup` + shows `lockIcon` + shows `hintLabel`
- **Unlocked:** click opens `popupId` popup + hides `lockIcon` + hides `hintLabel`

Reacts automatically to `App.OnUnlocked` — no scene reload or manual refresh needed.

## Key Files

### Created
- `Assets/App/Scripts/UI/SectionButton.cs` — 82 lines

## Verification

- `SectionButton` class declaration → line 5 ✓
- `RemoveAllListeners()` called before every `AddListener()` → line 63 ✓
- `requiresUnlock` field → line 13 ✓
- `OpenPasswordPopup` / `OpenTargetPopup` → lines 71, 76 ✓
- Commit: `51f405b`

## Decisions

| Decision | Rationale |
|----------|-----------|
| `RemoveAllListeners()` before every `AddListener()` in `Refresh()` | `App.OnUnlocked` fires each time a code is entered; without removal, listeners accumulate and the popup would open multiple times per click |
| `requiresUnlock = false` bypass path | Some sections (e.g., Programma) may be public — developer can disable the lock behavior per-button without needing a separate component |
| `_button` cached in `Awake()` | `GetComponent<Button>()` called once, not every `Refresh()` call |
| `string.IsNullOrEmpty(popupId)` guard | Prevents `PopupManager.Show("")` crash if developer forgets to set the popup id |

## Self-Check: PASSED
