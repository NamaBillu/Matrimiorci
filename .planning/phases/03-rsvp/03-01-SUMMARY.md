# Phase 03-01 Summary: GuestRowUI — Per-Guest Row Component

**Executed:** 2026-04-17
**Plan:** 03-01-PLAN.md
**Status:** Complete

## What Was Built

`GuestRowUI.cs` — a MonoBehaviour attached to the per-guest row prefab. Provides the public accessor surface that `RSVPPopup` reads at submit time.

## Artifacts

| File | Purpose |
|------|---------|
| `Assets/App/Scripts/UI/GuestRowUI.cs` | Per-guest row component with name pre-fill and read accessors |

## Key Decisions

- **No Unity lifecycle methods** — pure data accessor component; developer configures default Toggle states in the prefab Inspector (`meatToggle.isOn = true`, `attendingToggle.isOn = true`)
- **Null guards on all accessors** — `GuestName`, `IsAttending`, `SelectedMeal` all guard against unassigned Inspector references
- **`SelectedMeal` fallback** — returns `"non specificato"` if no toggle is on; the prefab default (meatToggle pre-selected) prevents this in practice

## Public API (for RSVPPopup — Plan 03-02)

```csharp
public string GuestName { get; }       // nameLabel.text
public bool IsAttending { get; }       // attendingToggle.isOn
public string SelectedMeal { get; }    // "carne" / "pesce" / "vegetariano" / "non specificato"
public void Initialize(string guestName);
```

## Commits

| Hash | Message |
|------|---------|
| a15c70e | feat(03-01): add GuestRowUI per-guest row component |

## Requirements Addressed

- RSVP-02 — Form pre-fills guest names (Initialize method)
- RSVP-03 — Attendance toggles exposed via IsAttending
- RSVP-04 — Meal preference accessible via SelectedMeal
