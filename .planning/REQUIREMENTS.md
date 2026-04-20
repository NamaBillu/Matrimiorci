# Requirements: Matrimiorci

**Defined:** April 17, 2026
**Core Value:** Every guest can access all wedding information and RSVP from a link, from any device, without installing anything.

## v1 Requirements

### Routing

- [ ] **ROUT-01**: Bootloader reads URL query param (`?type=home` vs `?type=invite`) to determine destination scene
- [ ] **ROUT-02**: If a valid password is already stored in PlayerPrefs, Bootloader always routes to Home regardless of URL param
- [ ] **ROUT-03**: Default routing (no param, or in Editor) goes to Home scene

### Invite Scene

- [ ] **INV-01**: Invite scene presents a digital version of the wedding invitation
- [ ] **INV-02**: Invite scene has a prominent button ("Entra" / "Open Invitation") that loads the Home scene
- [ ] **INV-03**: Invite scene is visually consistent with the physical invite design (same fonts, colors, wedding image)

### Password System

- [ ] **PASS-01**: A "Codice" (code) entry popup is accessible from the Home scene
- [ ] **PASS-02**: Password validation runs against a hardcoded dictionary of group codes (case-insensitive, trimmed)
- [ ] **PASS-03**: Successful password entry stores the code in PlayerPrefs (with explicit Save() call)
- [ ] **PASS-04**: Wrong password shows a friendly error message without revealing valid codes
- [ ] **PASS-05**: On app load, if a valid password is in PlayerPrefs the app restores the unlocked state automatically (no re-entry needed)

### Content Gating

- [ ] **GATE-01**: Locked-visible content (visible but overlaid) is revealed after any valid password is entered
- [ ] **GATE-02**: Hidden content (not visible at all) is shown only after a valid password is entered
- [ ] **GATE-03**: Each `ContentGate` component reacts to a global `App.OnUnlocked` event — no central manager needed
- [ ] **GATE-04**: Locked state is visually communicated (overlay with lock icon + "inserisci il tuo codice" hint)

### Home Scene — Wedding Information

- [ ] **INFO-01**: Wedding date, time, and full venue name/address displayed
- [ ] **INFO-02**: Venue map or directions link (opens Google Maps in browser)
- [ ] **INFO-03**: Wedding day schedule / timeline of events
- [ ] **INFO-04**: Dress code information
- [ ] **INFO-05**: Accommodation suggestions (locked — accessible only with code)
- [ ] **INFO-06**: FAQ section (minimum 5 questions covering common guest concerns)
- [ ] **INFO-07**: Contact information for the couple

### Personalized Section

- [ ] **PERS-01**: After password entry, a personalized section shows the guest group's names
- [ ] **PERS-02**: For groups with reserved apartments: a "breakfast preference" note/field is shown
- [ ] **PERS-03**: Personalized section is hidden until unlocked (GATE-02)

### RSVP

- [ ] **RSVP-01**: RSVP form is accessible from Home scene (popup or dedicated section), locked behind password
- [ ] **RSVP-02**: RSVP form pre-fills guest names from the group data (one row per person in the group)
- [ ] **RSVP-03**: Each guest has yes/no attendance toggles
- [ ] **RSVP-04**: RSVP form includes meal preference selection (meat / fish / vegetarian)
- [ ] **RSVP-05**: RSVP form includes dietary restrictions / allergies free text field
- [ ] **RSVP-06**: RSVP form includes a general free notes field
- [ ] **RSVP-07**: For apartment groups: RSVP form shows a breakfast preference field (options TBD by couple)
- [ ] **RSVP-08**: Submit button sends all data (group code + attendees + answers) via UnityWebRequest GET to Google Apps Script endpoint
- [ ] **RSVP-09**: While submission is in flight: submit button disabled, loading spinner shown
- [ ] **RSVP-10**: On submission success: confirmation message shown, re-submission prevented (PlayerPrefs flag)
- [ ] **RSVP-11**: On submission failure: error message shown with retry option (timeout set to 15s for venue Wi-Fi)

### Technical / Build

- [ ] **TECH-01**: WebGL build with Brotli compression enabled
- [ ] **TECH-02**: Build size target ≤ 12MB compressed
- [ ] **TECH-03**: Initial memory heap set to 32MB (not Unity default 256MB)
- [ ] **TECH-04**: WebAssembly 2023 + BigInt enabled
- [ ] **TECH-05**: All AudioClips use CompressedInMemory (iOS Silent Mode compatibility)
- [ ] **TECH-06**: Safe area support active for iPhone notch/home bar
- [ ] **TECH-07**: App tested on iOS Safari and Android Chrome before going live
- [ ] **TECH-08**: WebGL build hosted on HTTPS (required for QR code scanning)

## v2 Requirements

### Photo/Video Upload (Post-Wedding)

- **MEDIA-01**: Authenticated guests can upload photos and videos from the Home scene
- **MEDIA-02**: Free storage solution (Cloudinary free tier or Google Photos shared album link)
- **MEDIA-03**: Per-group upload folders or single shared album
- **MEDIA-04**: Guests can view a gallery of uploaded media in-app (optional)

### Registry / Gift

- **GIFT-01**: Gift registry or cash preference link displayed (cultural fit TBD)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Admin panel / RSVP dashboard in-app | Google Sheets view is sufficient for <50 guests |
| Email notifications on RSVP | Manual check of Google Sheets is fine at this scale |
| Password reset / self-service code retrieval | Hardcoded, couple distributes codes manually |
| Multi-language (English etc.) | Italian-only for this wedding |
| Photo upload at launch | Post-wedding feature, saves build time |
| Story / countdown timer | Anti-feature — wastes build time, dated by wedding day |
| User accounts / sign-up flow | PlayerPrefs + code is the auth model |
| Multi-step RSVP wizard | Single-screen form is sufficient and faster to build |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ROUT-01 | Phase 1 — Foundation | Pending |
| ROUT-02 | Phase 1 — Foundation | Pending |
| ROUT-03 | Phase 1 — Foundation | Pending |
| PASS-01 | Phase 1 — Foundation | Pending |
| PASS-02 | Phase 1 — Foundation | Pending |
| PASS-03 | Phase 1 — Foundation | Pending |
| PASS-04 | Phase 1 — Foundation | Pending |
| PASS-05 | Phase 1 — Foundation | Pending |
| GATE-01 | Phase 1 — Foundation | Pending |
| GATE-02 | Phase 1 — Foundation | Pending |
| GATE-03 | Phase 1 — Foundation | Pending |
| GATE-04 | Phase 1 — Foundation | Pending |
| INV-01 | Phase 2 — Wedding Content | Pending |
| INV-02 | Phase 2 — Wedding Content | Pending |
| INV-03 | Phase 2 — Wedding Content | Pending |
| INFO-01 | Phase 2 — Wedding Content | Pending |
| INFO-02 | Phase 2 — Wedding Content | Pending |
| INFO-03 | Phase 2 — Wedding Content | Pending |
| INFO-04 | Phase 2 — Wedding Content | Pending |
| INFO-05 | Phase 2 — Wedding Content | Pending |
| INFO-06 | Phase 2 — Wedding Content | Pending |
| INFO-07 | Phase 2 — Wedding Content | Pending |
| PERS-01 | Phase 2 — Wedding Content | Pending |
| PERS-02 | Phase 2 — Wedding Content | Pending |
| PERS-03 | Phase 2 — Wedding Content | Pending |
| RSVP-01 | Phase 3 — RSVP | Pending |
| RSVP-02 | Phase 3 — RSVP | Pending |
| RSVP-03 | Phase 3 — RSVP | Pending |
| RSVP-04 | Phase 3 — RSVP | Pending |
| RSVP-05 | Phase 3 — RSVP | Pending |
| RSVP-06 | Phase 3 — RSVP | Pending |
| RSVP-07 | Phase 3 — RSVP | Pending |
| RSVP-08 | Phase 3 — RSVP | Pending |
| RSVP-09 | Phase 3 — RSVP | Pending |
| RSVP-10 | Phase 3 — RSVP | Pending |
| RSVP-11 | Phase 3 — RSVP | Pending |
| TECH-01 | Phase 4 — WebGL Build & QA | Pending |
| TECH-02 | Phase 4 — WebGL Build & QA | Pending |
| TECH-03 | Phase 4 — WebGL Build & QA | Pending |
| TECH-04 | Phase 4 — WebGL Build & QA | Pending |
| TECH-05 | Phase 4 — WebGL Build & QA | Pending |
| TECH-06 | Phase 4 — WebGL Build & QA | Pending |
| TECH-07 | Phase 4 — WebGL Build & QA | Pending |
| TECH-08 | Phase 4 — WebGL Build & QA | Pending |

**Coverage:**
- v1 requirements: 44 total (ROUT×3, PASS×5, GATE×4, INV×3, INFO×7, PERS×3, RSVP×11, TECH×8)
- Mapped to phases: 44
- Unmapped: 0 ✓

> Note: The count of 44 reflects the full enumeration of individually listed requirements. The "41" figure in earlier documents was a miscalculation.

---
*Requirements defined: April 17, 2026*
*Last updated: April 17, 2026 after initialization*
