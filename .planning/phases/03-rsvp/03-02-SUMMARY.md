# Phase 03-02 Summary: RSVPPopup + RSVPSheet.gs

**Executed:** 2026-04-17
**Plan:** 03-02-PLAN.md
**Status:** Complete

## What Was Built

### RSVPPopup.cs
Full RSVP popup Popup subclass. Handles the complete RSVP flow: form population from `CurrentGroup`, per-guest row instantiation via `GuestRowUI`, submission coroutine, success/failure UX, and permanent submitted state via PlayerPrefs.

### RSVPSheet.gs
Google Apps Script `doGet` handler. Reads flat indexed URL params (`guest0Name`, `guest0Attendance`, `guest0Meal`, ...), applies formula injection sanitization, and appends a new row to the active sheet.

## Artifacts

| File | Purpose |
|------|---------|
| `Assets/App/Scripts/Popup/RSVPPopup.cs` | Full RSVP popup: form, submission, confirmation |
| `.planning/phases/03-rsvp/RSVPSheet.gs` | Apps Script handler (developer pastes into Code.gs) |

## Key Decisions

- **GET not POST** — locked decision from STATE.md; POST bodies dropped on Apps Script 302 redirect
- **Flat indexed params** — `guest0Name=X&guest0Attendance=si&guest0Meal=carne&guest1Name=Y...` — most debuggable, well under 2048-char URL limit for ≤10 guests
- **`Uri.EscapeDataString()`** — applied to every URL param value (Italian names, notes); `WWW.EscapeURL()` removed in Unity 2022+, not used
- **`request.timeout = 15`** — typed as `int` (not `15f`); 15 seconds per RSVP-11
- **`using UnityWebRequest request = ...`** — C# 8 using declaration; auto-disposes on coroutine exit
- **`PlayerPrefs.SetInt(SubmittedKey, 1); PlayerPrefs.Save()`** — immediate save on success; WebGL tab close does not fire `OnApplicationQuit`
- **Coroutine cancel in `OnHiding()`** — `StopCoroutine(_submitCoroutine)` prevents NullRef on deactivated GameObject
- **Formula injection guard** — `sanitize()` in Apps Script prepends `'` to values starting with `=`, `+`, `-`, `@`
- **`base.OnHiding()` always called** — required for `PopupManager.OnPopupHiding` notification

## Developer Setup Required

1. Open RSVP Google Sheet → Extensions → Apps Script
2. Paste `.planning/phases/03-rsvp/RSVPSheet.gs` contents into Code.gs
3. Deploy as Web App: Execute as Me / Who has access: **Anyone** (not authenticated)
4. Copy deployment URL into Unity Inspector → RSVPPopup → App Script Url
5. Register RSVPPopup prefab in PopupManager with id `"RSVPPopup"`
6. Wire all Inspector fields on the RSVPPopup prefab (formPanel, confirmationPanel, submitButton, spinnerObject, feedbackLabel, guestRowContainer, guestRowPrefab, breakfastPanel, breakfastToggles, notesInput)

## Commits

| Hash | Message |
|------|---------|
| 1188913 | feat(03-02): add RSVPPopup RSVP popup with submission coroutine |
| 5551fd3 | feat(03-02): add RSVPSheet.gs Google Apps Script handler |

## Requirements Addressed

- RSVP-01 — Accessible from Home after valid code (SectionButton with requiresUnlock=true, popupId="RSVPPopup")
- RSVP-02 — Pre-fills group member names (PopulateGuestRows from CurrentGroup.memberNames)
- RSVP-03 — Yes/no attendance (GuestRowUI.IsAttending — attendingToggle/notAttendingToggle)
- RSVP-04 — Meal preference (GuestRowUI.SelectedMeal — carne/pesce/vegetariano ToggleGroup)
- RSVP-05 — Dietary restrictions in shared notes field (notesInput)
- RSVP-06 — General notes field (same notesInput, 300 char limit)
- RSVP-07 — Breakfast preference for apartment groups (breakfastPanel + breakfastToggles, gated on hasBreakfastPref)
- RSVP-08 — GET submission to Google Apps Script (BuildSubmitUrl + UnityWebRequest.Get)
- RSVP-09 — Submit button disabled + spinner during flight (SubmitRSVP coroutine)
- RSVP-10 — Success confirmation + re-submission prevention (PlayerPrefs.SetInt + SetPanelActive)
- RSVP-11 — Error message with retry + 15s timeout (feedbackLabel + request.timeout = 15)
