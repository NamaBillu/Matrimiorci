# Matrimiorci — Roadmap

**Milestone:** v1.0 — Wedding Launch
**Total phases:** 4
**Requirements:** 41 v1 requirements
**Granularity:** Coarse (2-week deadline)
**Last updated:** April 17, 2026

---

## Phases

- [ ] **Phase 1: Foundation** — App routing + password system + content gating infrastructure
- [ ] **Phase 2: Wedding Content** — Home scene information + Invite scene + personalized sections
- [ ] **Phase 3: RSVP** — Full RSVP form + Google Sheets submission
- [ ] **Phase 4: WebGL Build & QA** — Build optimization + cross-browser verification

---

## Phase Details

### Phase 1: Foundation
**Goal:** URL routing, password entry, and content gating all work end-to-end — the infrastructure every other feature depends on
**Depends on:** Nothing (first phase)
**Requirements:** ROUT-01, ROUT-02, ROUT-03, PASS-01, PASS-02, PASS-03, PASS-04, PASS-05, GATE-01, GATE-02, GATE-03, GATE-04
**Success Criteria** (what must be TRUE):
  1. Typing `?type=invite` in the browser navigates to Invite scene; `?type=home` navigates to Home; no param defaults to Home
  2. A returning guest with a valid code in PlayerPrefs is always routed to Home regardless of URL param
  3. The "Codice" popup is accessible from Home; entering a valid code unlocks content and the state persists across page refreshes
  4. Entering a wrong code shows a friendly Italian error message with no hints about valid codes
  5. Locked-visible content shows the lock overlay with hint text; hidden content is invisible until a valid code is entered
**Plans:** 2 plans
Plans:
- [x] 01-01-PLAN.md — GroupData model + App.cs routing, unlock logic, session restore (Wave 1)
- [x] 01-02-PLAN.md — PasswordPopup + ContentGate components (Wave 2)

### Phase 2: Wedding Content
**Goal:** All wedding information is displayed in Home and the Invite scene is complete — guests can find every piece of day-of info
**Depends on:** Phase 1
**Requirements:** INV-01, INV-02, INV-03, INFO-01, INFO-02, INFO-03, INFO-04, INFO-05, INFO-06, INFO-07, PERS-01, PERS-02, PERS-03
**Success Criteria** (what must be TRUE):
  1. Home scene displays wedding date, time, venue name, full address, and a Google Maps deep link
  2. Home scene includes ceremony + reception schedule, dress code, FAQ (≥5 questions), and contact info
  3. Accommodations section is locked behind the password gate (visible overlay, content hidden until code entered)
  4. After entering a valid code, a personalized section shows the guest group's names; apartment groups also see a breakfast note
  5. Invite scene presents the digital invitation with matching fonts/colors and a prominent "Entra" button that loads Home
**Plans:** 4 plans

Plans:
- [x] 02-01-PLAN.md — GroupData extension: apartment fields (Wave 1)
- [x] 02-02-PLAN.md — GroupContentGate: per-group visibility component (Wave 1)
- [x] 02-03-PLAN.md — SectionButton: two-state lock/unlock button (Wave 2)
- [x] 02-04-PLAN.md — AlloggioPopup: accommodation display with apartment variant (Wave 2)

### Phase 3: RSVP
**Goal:** Guests can complete and submit their RSVP from within the app and the data lands in Google Sheets
**Depends on:** Phase 2
**Requirements:** RSVP-01, RSVP-02, RSVP-03, RSVP-04, RSVP-05, RSVP-06, RSVP-07, RSVP-08, RSVP-09, RSVP-10, RSVP-11
**Success Criteria** (what must be TRUE):
  1. RSVP form is accessible from Home only after a valid code is entered
  2. Form pre-fills guest names for the group; each guest has yes/no attendance, meal preference, and dietary/notes fields
  3. Apartment groups see an additional breakfast preference field
  4. Tapping Submit disables the button, shows a loading spinner, and sends data to Google Sheets via GET request; a new row appears in the sheet
  5. Success shows a full confirmation message and prevents re-submission; failure shows an error with a retry option
**Plans:** 2 plans
Plans:
- [x] 03-01-PLAN.md — GuestRowUI: per-guest row component with attendance and meal accessors (Wave 1)
- [x] 03-02-PLAN.md — RSVPPopup + RSVPSheet.gs: full popup + submission coroutine + Apps Script handler (Wave 2)

### Phase 4: WebGL Build & QA
**Goal:** The build is production-ready: compressed, small, and verified working on every target device before go-live
**Depends on:** Phase 3
**Requirements:** TECH-01, TECH-02, TECH-03, TECH-04, TECH-05, TECH-06, TECH-07, TECH-08
**Success Criteria** (what must be TRUE):
  1. WebGL build compiles with Gzip compression; total download size is ≤ 12MB
  2. Memory heap is set to 32MB; all AudioClips use CompressedInMemory; WebAssembly 2023 + BigInt are enabled
  3. App loads correctly and all features work on iOS Safari and Android Chrome
  4. Build is deployed to HTTPS hosting; QR code scan from a physical device opens the app correctly
**Plans:** 3 plans
Plans:
- [x] 04-01-PLAN.md — SetAudioCompression Editor script + Player Settings configuration (Wave 1)
- [ ] 04-02-PLAN.md — WebGL build + GitHub Pages gh-pages deployment (Wave 2)
- [ ] 04-03-PLAN.md — QA checklist: Windows browsers + Android + iOS physical devices (Wave 3)

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 0/2 | Not started | - |
| 2. Wedding Content | 0/TBD | Not started | - |
| 3. RSVP | 0/TBD | Not started | - |
| 4. WebGL Build & QA | 0/TBD | Not started | - |

---

## Backlog (v2)

- **MEDIA-01–04** — Post-wedding photo/video upload for guests (Cloudinary or Google Photos)
- **GIFT-01** — Gift registry or cash preference link (cultural fit TBD)

---

*Roadmap created: April 17, 2026*
