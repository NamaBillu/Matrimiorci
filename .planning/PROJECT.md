# Matrimiorci

## What This Is

A Unity WebGL wedding app accessible from any browser (desktop and mobile). Guests arrive via QR code (physical invite) or a personalized digital link and find all wedding information in one place. A password system unlocks personalized content and RSVP functionality for each guest group.

## Core Value

Every guest can access all wedding information and RSVP from a link, from any device, without installing anything.

## Requirements

### Validated

- ✓ Scene navigation system (Bootloader → target scene via SceneManager) — existing
- ✓ Popup show/hide system with UIAnimationStateMachine — existing
- ✓ Sound manager with music/SFX toggle and PlayerPrefs persistence — existing
- ✓ Safe area support for mobile browsers (notch/home indicator) — existing
- ✓ Typography design system (CrimsonText serif + Slight decorative) — existing
- ✓ Singleton manager architecture (App, PopupManager, SoundManager) — existing

### Active

- [ ] URL query param routing: `?type=home` sends to Home, `?type=invite` sends to Invite
- [ ] Invite scene: password-already-stored guests skip Invite and go to Home
- [ ] Invite scene: "View invitation" experience with button to enter the Home scene
- [ ] Password entry popup accessible from Home scene
- [ ] Password validation against hardcoded group map
- [ ] Password stored in PlayerPrefs after successful entry
- [ ] Locked content gates: content visible but blurred/covered until password entered
- [ ] Hidden content gates: content entirely hidden until password entered
- [ ] Per-group personalized display: names + group-specific info shown after password
- [ ] RSVP form: pre-filled names, attendance yes/no toggles, meal preference, dietary restrictions, free notes field
- [ ] Per-group optional RSVP field: breakfast preference (for apartment guests)
- [ ] RSVP submission to Google Sheets via Apps Script webhook
- [ ] All wedding information displayed in Home scene (venue, date, schedule, etc.)
- [ ] Post-wedding photo/video upload for guests (deferred — see v2)

### Out of Scope

- Admin panel to manage passwords or view RSVPs in-app — use Google Sheets directly
- Email/push notifications on RSVP — Google Sheets handles that manually
- Password change or reset flow — hardcoded, no self-service
- Multi-language support — Italian only
- Photo/video upload at launch — post-wedding feature (v2)
- Server-side auth — PlayerPrefs + hardcoded map is sufficient at this scale

## Context

- **Stack:** Unity 6 (6000.3.5f2), WebGL build target, C#, UGUI, UIAnimations (NamaBillu), DOTween
- **Scale:** <10 password groups, <50 total guests — hardcoding all passwords is correct
- **Existing scenes:** Bootloader (app init + routing), Home (wedding info), Invite (digital invitation)
- **URL routing:** Query params parsed in Bootloader via `Application.absoluteURL` (Unity WebGL)
- **Persistence:** PlayerPrefs (browser localStorage in WebGL builds)
- **RSVP backend:** Google Sheets + Google Apps Script (free HTTP POST endpoint)
- **Deadline:** 2 weeks to go live

## Constraints

- **Timeline:** 2 weeks — ship MVP, defer everything non-essential
- **Tech stack:** Unity 6 WebGL fixed — no switching engines
- **Budget:** Zero — all external services must be free tier forever at this scale
- **Browser target:** Desktop browsers + mobile browsers (iOS Safari, Android Chrome)
- **Scale:** <10 groups, <50 guests — no scalability optimization needed

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Hardcode all passwords | <10 groups, 2-week deadline — no CMS needed | — Pending |
| Google Sheets + Apps Script for RSVP | Free, zero infra, easy to read results | — Pending |
| PlayerPrefs for password storage | WebGL = localStorage, no server auth needed at this scale | — Pending |
| Query param for URL routing (`?type=home/invite`) | Simple, QR-codeable, parseable in WebGL | — Pending |
| Single password tier (any valid = fully unlocked) | Keeps logic simple, sufficient for family groups | — Pending |
| Photo upload deferred to post-wedding (v2) | Not needed at launch, saves 2 weeks | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: April 17, 2026 after initialization*
