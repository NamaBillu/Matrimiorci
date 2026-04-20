# Feature Landscape — Wedding App (Matrimiorci)

**Domain:** Small-scale wedding information + RSVP app (Unity 6 WebGL)
**Audience:** <50 guests, Italian-speaking, mixed digital literacy (includes 60+ year olds)
**Researched:** April 2026
**Confidence:** HIGH (wedding app patterns are mature and well-documented)

---

## Table Stakes

Features guests universally expect. Absence makes the app feel incomplete or broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Date + Time | First thing every guest checks | Low | Display prominently, not buried |
| Venue name + address | Guests need to navigate there | Low | With Google Maps deep link button |
| Ceremony schedule | Guests need to know when to arrive | Low | Even rough timing (15:00 ceremony, 19:00 dinner) |
| Dress code | Guests ask this constantly | Low | One line is enough; absence causes anxiety |
| RSVP form | The entire reason to authenticate | Medium | Pre-filled names, yes/no per person, meal preference |
| Contact for day-of questions | "Where do I park?" "Is it wheelchair accessible?" | Low | Couple or designated point person |
| Accommodations info | Out-of-town guests need this early | Low | List of nearby hotels or a single block recommendation |
| Parking/transport info | Most guests drive; this is asked constantly | Low | A note on the venue section is sufficient |

### What makes a wedding app better than a PDF

A PDF covers static information (venue, date, schedule). The app earns its existence through:

1. **RSVP submission** — data actually lands in Google Sheets, no phone calls needed
2. **Map deep link** — taps open Google Maps / Apple Maps navigation directly
3. **Personalization** — guests see their own names and group-specific info after unlocking
4. **Updateable** — if the venue changes, update the WebGL build, QR stays the same

If none of these are present, a well-formatted PDF invitation does the same job.

---

## Differentiators

Nice-to-have features that improve experience without being expected. Build only if time allows.

| Feature | Value Proposition | Complexity | When to Add |
|---------|-------------------|------------|-------------|
| Pre-filled RSVP names | Reduces friction significantly for all ages | Low | Phase 1 — trivial with password-group mapping |
| Per-group specific info | Apartment guests see breakfast question; others don't | Low | Phase 1 — already planned |
| Animated invitation reveal | Makes the digital invite feel special vs a webpage | Medium | Only if Invite scene design time allows |
| FAQ section | Reduces day-of messages to the couple | Low | Good time investment; write 5-7 questions |
| Post-wedding photo sharing link | Guests want to see and share memories | Low | v2 — just a button that opens Google Photos shared album |
| Map embedded or linked for each venue | Removes friction for car-dependent guests | Low | Add Google Maps deep link, not an embedded map |
| Soft reminder of RSVP deadline | Guests forget — show a subtle "RSVP by [date]" | Trivial | Add to RSVP section header |

### Post-Wedding Photo Sharing — Recommended Approach

**Recommendation: Google Photos Shared Album link button (v2)**

| Option | Cost | Guest Friction | Upload Experience | Unity Integration |
|--------|------|----------------|-------------------|-------------------|
| Google Photos shared album | Free | Low — no account required to view; Google account to upload | Native, familiar | `Application.OpenURL(url)` — trivial |
| Dropbox folder upload link | Free (2 GB tier) | Medium — unfamiliar UI | Upload-only flow | `Application.OpenURL(url)` — trivial |
| WeTransfer | Free (2 GB) | Medium — file manager paradigm, not photo-oriented | Clunky for phone photos | `Application.OpenURL(url)` — trivial |
| Dedicated wedding apps (WedPics, Foto) | Freemium | High — requires app install | Good, but app install is a barrier | Can't deep-link from Unity |
| In-app Unity upload | Free (needs backend) | Low once built | Custom | Medium-High (multipart HTTP, CORS, storage) |

**Decision:** Google Photos shared album is the correct choice for this project — free forever at this scale, guests can upload without an account (via link), deeply familiar, and the Unity implementation is a single `Application.OpenURL()` call. Build as a post-wedding button in v2.

---

## Anti-Features

Features that seem appealing but actively hurt UX or waste build time at this scale.

### Anti-Feature 1: Couple Story / "Our Story" Section

**Why it seems good:** Every major wedding platform (Zola, The Knot, Joy) includes it.
**Why it hurts:** Guests read it once and never return. In a 2-week build window it competes with RSVP and schedule — both of which guests use multiple times. Family members already know the story.
**Instead:** One paragraph of copy on the main screen is sufficient. No dedicated section.

### Anti-Feature 2: Social Media Hashtag Wall

**Why it seems good:** Trendy, encourages participation, provides live content.
**Why it hurts:** Requires Twitter/Instagram API integration, moderation to prevent spam, and depends on guests actually using hashtags — which 60+ year old Italian guests generally don't. Zero guests will use it.
**Instead:** If sharing is desired, just add the hashtag as text somewhere on the app.

### Anti-Feature 3: Photo Upload During the Event (v1)

**Why it seems good:** Guests can share memories in real-time.
**Why it hurts:** Requires stable venue WiFi during a high-attendance event, guests are distracted during the ceremony, and live uploads from guests are unpredictable in quality/content. Also requires build time (multipart upload + storage).
**Instead:** Defer to v2. Post-wedding link is safer, better quality, and trivially easy.

### Anti-Feature 4: Live Countdown Timer

**Why it seems good:** Creates anticipation, shows the date prominently.
**Why it hurts:** Pure novelty. Once the countdown reaches zero, it breaks. Guests who open the app at the event see "0 days, 0 hours" which is bizarre. Adds real-time state management for no practical value.
**Instead:** Display the date and time as static text.

### Anti-Feature 5: Multi-Step RSVP Wizard

**Why it seems good:** Feels structured; guides users through decisions.
**Why it hurts:** Increases the number of taps for a simple yes/no. Every extra screen is a dropout point, especially for older guests on mobile. The RSVP data needed (attendance + meal preference + dietary note) fits in a single scroll.
**Instead:** Single-screen RSVP form. All fields visible at once, single submit button.

### Anti-Feature 6: Password Show/Hide Toggle for All Group Codes

**Why it seems good:** Helps users who mistyped.
**Why it hurts:** If passwords are visible as typed, a third party near the phone can see the code. More importantly, showing all possible passwords creates the wrong expectation — the app should reveal one group's data, not let guests browse all groups.
**Instead:** Show/hide toggle only for the current code field (standard input[type=password] behavior).

### Anti-Feature 7: Guest Guestbook / Comments

**Why it seems good:** Feels interactive and celebratory.
**Why it hurts:** Requires moderation, a writable backend, and spam handling. Inappropriate messages from (drunk) guests are a real risk. At <50 guests you already know everyone.
**Instead:** If wishes are desired, guests can text the couple directly.

### Anti-Feature 8: Registry Link

**Why it seems good:** Standard on English-language wedding platforms.
**Why it hurts:** In Italian wedding culture, cash gifts (busta) are the strong norm. A digital registry link can come across as impersonal or presumptuous in this context. Confirm with the couple before including.
**Instead:** If needed, a simple text mention in the FAQ ("per informazioni su regali...") is less transactional.

### Anti-Feature 9: Geolocation / "Navigate to Venue" Button with In-App Map

**Why it seems good:** Integrated navigation seems seamless.
**Why it hurts:** Browser geolocation prompts are disorienting on mobile, especially when the guest is not yet near the venue. Embedded maps (Google Maps iFrame) don't work in WebGL builds without platform-specific handling.
**Instead:** Deep link button: `Application.OpenURL("https://maps.google.com/?q=VenueAddress")`. Opens the guest's native maps app directly. One tap, zero permissions required.

---

## RSVP UX Patterns — Design Guidance

Based on NNG form design research and wedding app ecosystem patterns.

### What works for older/less technical guests

1. **Large tap targets** — Attendance toggle buttons should be at minimum 44×44px (Apple HIG), ideally 56px+ for comfort
2. **Binary attendance UI** — Two large buttons ("Parteciperò ✓" / "Non potrò esserci ✗") rather than a checkbox or dropdown
3. **Single column layout** — Never two columns on mobile; vertical scanning only
4. **Pre-filled name labels** — Guest sees "Ciao Marco e Sofia!" before form fields. Reduces confusion about who is RSVPing.
5. **No placeholder text in fields** — Placeholder text disappears when typing, which confuses older users who forget what the field is for. Use above-field labels instead.
6. **Visible success state** — A full-screen (or near full-screen) success confirmation after submit. Not a small toast. "RSVP ricevuto! Grazie ❤" prominently displayed.
7. **No account creation** — Password entry already serves as authentication. Requiring email on top is a dropout trigger.
8. **Dedicated section, not a popup** — RSVP should live in a scrollable section. Popups require managing z-index, animation timing, and close-button positioning — all of which become frustrating on mobile Safari. A full-panel RSVP section has fewer edge cases.

### One-submit vs editable

**Recommendation: One-submit with "contact us to change" instruction.**

Editable RSVP (allow re-open and edit) requires:
- Storing submission state in PlayerPrefs and syncing it against Google Sheets state
- Handling partial edits (attendance changes but meal preference stays)
- Edge case: guest submits "Yes" then changes to "No" — both rows appear in Google Sheets

At <50 guests, if a guest needs to change their RSVP, they text the couple. This is normal and expected. Build complexity for a case that affects 1-2 guests maximum.

**Show RSVP section as read-only (submitted values visible, no edit button) after first submit.** Keeps it simple, still lets guests verify what they submitted.

---

## Password/Gate UX — Design Guidance

### Principles for non-technical users

1. **Call it "il tuo codice" not "password"** — "Password" implies account creation and security implications. "Codice" (code) is understood as something simple on an invite.
2. **Tell users where to find the code** — Add helper text: "Trovi il codice sul tuo invito cartaceo" (Find the code on your paper invitation). Without this, guests who have lost the invitation are stuck.
3. **Short codes over long ones** — 4-6 character codes minimize transcription errors. "FIAM01" is easier to type correctly than "FAMIGLIA_MANCINI_01".
4. **Case-insensitive matching** — Mobile auto-capitalize creates "Fiam01" when guests mean "fiam01". Always normalize to lowercase before comparison. This is the #1 source of wrong-password failures.
5. **No lockout on failed attempts** — This is a family wedding, not a bank. Lockouts cause distress and support calls. Show a friendly error: "Codice non trovato. Controlla l'invito."
6. **Show/hide toggle on the code field** — Older users mistype because they can't see what they're entering. A "Mostra" toggle reduces typo frustration.
7. **No obscure error messages** — "Authentication failed" is wrong. "Codice non riconosciuto — riprova" is correct.
8. **Autofocus + bring up numeric/alphanumeric keyboard** — On mobile, the code field should auto-focus so the keyboard appears without an extra tap. Use `inputType = "text"` (not "password") with the show/hide toggle implemented in C#, not relying on browser behavior.

### Wrong password flow

```
Guest enters wrong code
  → Show inline error below field: "Codice non trovato. Riprova."
  → Clear the field (don't leave the wrong code visible — it's confusing)
  → Keep focus on the input field
  → After 3 failed attempts: add helper text "Hai perso il tuo invito? Contatta [nome sposa/o] al [numero]"
```

Do NOT: show a modal/popup on wrong password. Do NOT redirect to a different screen. Stay on the same input, inline error only.

---

## Content Hierarchy — What Guests Actually Look At

Based on usage patterns from Joy, Zola, and The Knot analytics (industry-reported, not primary data).

### Viewing frequency (high to low)

1. **Event details** (date, time, venue, address) — Referenced repeatedly, especially the week before
2. **RSVP** — Used once but is the highest-stakes interaction
3. **Schedule** — Checked the day of and the day before
4. **Accommodations** — Checked early when planning travel, rarely after
5. **FAQ** — Checked when something is unclear, mostly before the event
6. **Transport/Parking** — Checked the day of
7. **Dress code** — Checked once, then forgotten
8. **Contact** — Checked only when something goes wrong

### Locked vs Public content — visual treatment

**Problem:** If locked content is fully hidden, guests don't know it exists and skip the authentication step.
**Problem:** If locked content is blurred, it looks broken or unfinished on mobile.

**Recommended pattern:**
- **Public sections:** Full access, no treatment needed (venue, schedule, FAQ)
- **Locked sections:** Show a distinct card/panel that reads "Contenuto personalizzato 🔒 — Inserisci il tuo codice per vedere i dettagli del tuo gruppo e confermare la tua presenza." This communicates: (a) there's something worth unlocking, (b) what the unlock achieves, (c) how to do it.
- **After unlock:** Replace the lock card with the RSVP form and personalized content inline. No page reload.

**Don't blur content behind a frosted overlay.** It reads as a loading error on mobile and frustrates users who think the page is broken.

---

## MVP Feature Checklist for 2-Week Build

### Must ship (Table Stakes)

- [x] Date + time + venue name + address
- [x] Google Maps deep link button
- [x] Ceremony and reception schedule (even rough timing)
- [x] Dress code (one line)
- [x] Accommodation info (list of nearby hotels or a block)
- [x] Contact info for day-of questions
- [x] Password entry (code, not password language)
- [x] Pre-filled RSVP form (names from group map)
- [x] Attendance yes/no per person
- [x] Meal preference per person
- [x] Dietary restrictions field (free text)
- [x] RSVP submission to Google Sheets
- [x] Post-submit success state
- [x] "Locked content" panel prompting authentication
- [x] FAQ (5-7 questions; parking, kids, dress code, schedule, gifts)

### Ship if time permits

- [ ] Per-group extras (breakfast preference for apartment guests)
- [ ] RSVP deadline reminder text
- [ ] Show/hide toggle on code field
- [ ] Post-3-fails contact suggestion on password error

### Defer to v2

- [ ] Post-wedding photo album link button (Google Photos)
- [ ] Any video content
- [ ] Registry information (confirm cultural appropriateness first)

---

## Feature Dependencies

```
Password entry → RSVP form (RSVP requires authenticated state)
Password entry → Personalized names display (name comes from group map)
Google Sheets webhook → RSVP submission (RSVP requires backend endpoint)
Schedule → FAQ (FAQ references schedule details)
```

---

## Sources

- NNG Web Form Design Guidelines (nngroup.com) — MEDIUM confidence (verified, 2016, principles unchanged)
- The Knot destination wedding website content checklist — MEDIUM confidence (industry standard)
- Joy (withjoy.com) wedding website features — MEDIUM confidence (observed feature set)
- Unity WebGL `Application.OpenURL` documentation — HIGH confidence (official Unity API)
- Italian wedding cultural norms (busta/cash gifts, codice terminology) — MEDIUM confidence (training data, not verified via research tool; flag for couple confirmation)
- Google Photos shared album guest upload behavior — MEDIUM confidence (feature exists as of 2025; verify free tier still allows link-based upload at time of build)
