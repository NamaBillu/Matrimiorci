# Phase 04-03 — Manual QA — SUMMARY

**Status:** Complete
**Wave:** 3

## What Was Verified

Structured manual QA across all three device categories. All checks passed with no bugs found.

## QA Results

### Windows Browsers (Chrome, Firefox, Edge) ✅

- App loads without console errors
- Unity loading bar completes
- URL routing: `?type=invite` → Invite scene, no param → Home scene
- Invite scene: content displays correctly (fonts, colors, wedding image)
- Password flow: valid code unlocks and persists, invalid code shows Italian error
- Home scene: all content sections displayed (date/time/venue, maps, schedule, dress code, FAQ ≥5, contact, accommodations lock)
- Personalized sections: group-specific content gated correctly (breakfast field visible/hidden per group type)
- RSVP: name pre-fill, toggles, submit flow, post-submit confirmation — all working
- Sound effects: functional

### Android (physical device — Chrome) ✅

- App loads, Unity loading bar completes
- QR code scan → `?type=invite` → Invite scene loads
- Touch gestures work (tap, scroll)
- Password keyboard appears on code input tap
- Code entry → content unlocked
- RSVP form usable on touch keyboard, submit → success
- Audio plays correctly

### iOS (physical iPhone — Safari) ✅

- App loads, Unity loading bar completes
- Safe area respected (content not hidden behind notch or home indicator)
- QR scan with iPhone camera → Safari → Invite scene
- Touch input works throughout
- Code entry keyboard appears
- RSVP form scrollable, submission successful
- Silent Mode: app loads without crash
- Audio plays after first tap (iOS AudioContext unlock working)
- App resumes correctly after Home → return to tab

## Requirements Satisfied

- TECH-06: Safe area respected on notched iPhones ✅
- TECH-07: Tested on iOS Safari and Android Chrome — no blocking bugs ✅
