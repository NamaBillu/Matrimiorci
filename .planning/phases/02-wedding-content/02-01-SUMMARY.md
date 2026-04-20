---
plan: 02-01
phase: 02-wedding-content
status: complete
commit: f92ea7c
---

# Plan 02-01 Summary — GroupData Apartment Fields

## What Was Built

Extended `GroupData.cs` with four apartment-specific fields appended to the end of the `#region Inspector Variables` block:

- `hasApartment` (bool, default `false`) — controls which panel AlloggioPopup shows
- `apartmentName` (string) — display name for the apartment
- `apartmentAddress` (string) — full address
- `apartmentNotes` (List<string>) — per-item notes joined with `\n` in AlloggioPopup

All four fields carry `[Tooltip]` attributes per project convention. Existing fields (`password`, `groupDisplayName`, `memberNames`, `hasBreakfastPref`) are untouched.

## Key Files

### Modified
- `Assets/App/Scripts/Managers/GroupData.cs` — 4 new fields added after `hasBreakfastPref`

## Verification

- `grep hasApartment GroupData.cs` → line 23 ✓
- `grep hasBreakfastPref GroupData.cs` → line 20 ✓ (existing field unchanged)
- Commit: `f92ea7c`

## Decisions

| Decision | Rationale |
|----------|-----------|
| Fields added at END of #region block | Preserves Inspector field ordering for existing serialized GameObjects; Unity serialization is order-sensitive for existing scenes |
| `hasBreakfastPref` (Phase 1) distinct from `hasApartment` (Phase 2) | A group can have both an apartment AND breakfast pref (e.g., the wedding party); separate booleans avoid conflation |

## Self-Check: PASSED
