# Research Summary — Matrimiorci Unity 6 WebGL Wedding App

**Synthesized:** 2026-04-17
**Sources:** STACK.md · FEATURES.md · ARCHITECTURE.md · PITFALLS.md
**Stack is fully locked** — Unity 6 (6000.3.5f2), C#/IL2CPP, UGUI, DOTween, UIAnimations. No new dependencies needed.

---

## Stack Recommendation

- **Hosting → GitHub Pages.** `Application.absoluteURL` returns the full page URL including query params. No `.jslib` bridge needed. Itch.io breaks URL routing (iframe returns CDN URL, not guest URL).
- **RSVP backend → Google Apps Script GET to Sheets.** Zero cost, no server. Use GET with query params — NOT POST. POST bodies are silently dropped on the 302 redirect that Apps Script always returns.
- **URL routing → pure C# on `Application.absoluteURL`.** Parse `?type=invite` in `App.Start()`. Gate with `#if UNITY_WEBGL && !UNITY_EDITOR` — returns empty string in Editor.
- **Persistence → PlayerPrefs (IndexedDB in Unity 6 WebGL).** Call `PlayerPrefs.Save()` immediately after every write. Never rely on `OnApplicationQuit()` — it does not fire when a browser tab closes.
- **Build compression → Gzip for GitHub Pages** (Brotli requires custom server headers that GitHub Pages does not provide out of the box; Unity's decompression fallback handles it, but Gzip is simpler and reliable). Target total download: **< 12 MB**.

---

## Table Stakes Features

These must ship. Absence makes the app feel broken or pointless:

- **Date + time + venue name + address** — the single most-checked piece of info; display at top
- **Google Maps deep link** — `Application.OpenURL("https://maps.google.com/?q=...")`, one tap, zero permissions
- **Ceremony + reception schedule** — even rough timing (15:00 ceremony, 19:00 dinner)
- **Dress code** — one line; its absence causes constant messages to the couple
- **Contact info for day-of questions** — parking, accessibility, lost kids
- **Accommodations** — nearby hotels list or block recommendation
- **Password gate (called "il tuo codice")** — entry point to personalized content + RSVP
- **Pre-filled RSVP form** — shows group member names automatically after password unlock
- **Attendance yes/no per person** — large tap targets (56px+), binary buttons not checkboxes
- **Meal preference per person**
- **Dietary restrictions free-text field** — one field, keep keyboard usage minimal
- **RSVP submission to Google Sheets** — the core value proposition of the whole app
- **Full-screen success confirmation after submit** — "RSVP ricevuto! Grazie ❤" prominently displayed
- **Locked content panel** — visible card explaining what unlocking does, not a blur overlay

**MVP checklist (ship these, nothing else is mandatory):**
- Date / time / venue / address / maps link
- Schedule + dress code + accommodations + contact
- Password ("codice") entry with case-insensitive matching
- RSVP form: pre-filled names, yes/no per person, meal pref, dietary, notes
- Google Sheets submission with spinner and success/error states
- FAQ (5–7 questions: parking, kids, dress, schedule, gifts)

**Defer to v2:** post-wedding photo album button, video content, registry link.

**Do not build:** live countdown timer, multi-step RSVP wizard, social media hashtag wall, photo upload during event, in-app embedded map, guest guestbook.

---

## Architecture Blueprint

Critical patterns — deviating from these causes bugs that are hard to diagnose:

**Bootloader routing (in `App.Start()`):**
```
1. PlayerPrefs has saved password? → always route to Home (returning guest)
2. URL ?type=invite → Invite scene
3. Anything else → Home scene (safe default)
```
- PlayerPrefs check FIRST — returning guests skip Invite regardless of URL
- No async scene loading needed; Bootloader has a loading overlay

**Group data (in `App` Inspector, NOT ScriptableObject):**
- `List<GroupData>` serialized as Inspector list → converted to `Dictionary<string, GroupData>` in `Awake()` with `StringComparer.OrdinalIgnoreCase`
- `password.Trim()` before every comparison — mobile keyboards add leading/trailing spaces
- `App.CurrentGroup` set after unlock; `App.OnUnlocked` static event fires for all content gates
- `PlayerPrefs.Save()` called immediately in `TryUnlock()` — never deferred

**Content gating — `ContentGate` component (not a GateManager):**
- Subscribes to `App.OnUnlocked` in `OnEnable`, unsubscribes in `OnDisable`
- Calls `Refresh()` on `OnEnable` — handles gates that activate after unlock already happened
- Lock overlay is a **sibling** of content root, not a child — `SetActive(false)` on content must not hide the overlay
- Show locked content as a visible card/panel ("🔒 Inserisci il tuo codice..."), not a blurred overlay

**RSVP form — `RSVPFormPopup : Popup`:**
- Reuses existing `Popup` base class + `PopupManager` — no new popup infrastructure
- Reads `App.Instance.CurrentGroup` directly — no data passing needed (one active group at a time)
- Builds `RSVPMemberRow` prefabs dynamically from `group.memberNames`
- Submits via **GET** to Apps Script URL with query params (`UnityWebRequest.Get(url)`)
- Sets `request.timeout = 15` — required to prevent infinite hang on slow networks
- Shows spinner on submit, success panel on `Result.Success`, retry button on failure
- Marks form as submitted in PlayerPrefs immediately on success; re-shows as read-only on next open

**Audio (SoundManager):**
- Never call `AudioSource.Play()` in `Start()` or `Awake()` — iOS Safari blocks all audio until user gesture
- Gate first audio call on the loading screen's "tap to continue" button (`OnPointerDown`, not `OnPointerUp`)
- All AudioClips: `loadType = CompressedInMemory` — required for iOS Silent Mode compatibility

---

## Critical Watch-Outs

Top pitfalls that will silently break the app at the wedding if ignored:

**1. Audio completely silent on iOS Safari (C-1) — BLOCKING**
- Autoplay policy blocks all audio until user gesture. No workaround.
- Fix: first audio call must come from inside a button's `OnPointerDown` handler (the loading screen tap)
- Set all AudioClips to `CompressedInMemory` — `DecompressOnLoad` silently fails in iOS Silent Mode
- Test on a real iPhone with Silent Mode ON before launch

**2. RSVP submissions lost — Apps Script redirect trap (C-2) — BLOCKING**
- HTTP POST to `/exec` → 302 redirect → POST becomes GET → `doPost()` never runs → 0 rows in Sheets
- Fix: use GET with URL-encoded query params (`UnityWebRequest.Get(url + "?group=...&names=...")`); implement `doGet()` in Apps Script
- Always create a **new deployment version** after any script change (not edit existing)
- Test the live URL with a real browser GET before wiring to Unity

**3. PlayerPrefs lost in private browsing (C-3)**
- iOS Safari private mode clears IndexedDB on tab close; writes may fail silently
- Fix: call `PlayerPrefs.Save()` immediately after every `PlayerPrefs.Set*()` call — never defer to `OnApplicationQuit()`
- Design for re-entry: the password is short, re-typing it is a minor inconvenience, not a failure

**4. Build too large — guests abandon on mobile (M-1)**
- Default Unity 6 WebGL build: 20–40 MB uncompressed; loads in 30+ seconds on congested venue Wi-Fi
- Mandatory build settings: Development Build OFF · Code Optimization: Disk Size with LTO · Strip Engine Code ON · Managed Stripping Level: High · Gzip compression
- Target: `.wasm.gz` < 8 MB, `.data.gz` < 3 MB, total < 12 MB
- Verify with a real mobile device on mobile data before printing QR codes

**5. RSVP form layout broken by mobile keyboard (M-2)**
- When guest taps the dietary notes field on iPhone, virtual keyboard shrinks the viewport; Unity canvas may be partially pushed off-screen
- Fix: minimize free-text fields (notes/dietary is the only one); test on real iPhone with notes field open; consider `WebGLInput.mobileKeyboardSupport = false` if notes field is removed
- `TMP_InputField.HideMobileInput` has no effect on WebGL — don't rely on it

---

## Phase Implications

What the research implies about build order:

**Phase 1 — Foundation (must be first; everything depends on this)**
- Bootloader scene + `App` singleton: URL routing, GroupData dictionary, `TryUnlock()`, `OnUnlocked` event, `PlayerPrefs.Save()` on every write
- Loading screen with "tap to continue" button — this is the audio gate; must exist before any audio plays
- Editor `#if` guards for `Application.absoluteURL`
- Validates: routing works, password unlock works, group data populates correctly

**Phase 2 — Home Scene structure + content (parallel with Phase 3)**
- Static wedding info sections: date/time/venue/address, schedule, dress code, accommodations, contact, FAQ
- Google Maps deep link button
- `ContentGate` components on locked sections (visible lock card, not blur)
- No RSVP yet — content only

**Phase 3 — RSVP form + Google Sheets (parallel with Phase 2)**
- Google Apps Script deployed and tested in browser before writing Unity code
- `RSVPFormPopup` with dynamic member rows from `CurrentGroup`
- GET submission with 15s timeout, spinner, success/error states, retry button
- `hasBreakfastPref` conditional section for apartment guests
- Integration test: real submission lands in Sheets

**Phase 4 — Invite scene (can be last; no other features depend on it)**
- Animated invitation reveal for first-time guests routed via `?type=invite`
- Transitions to Home after reveal
- Lower priority than RSVP — ship only after Phases 1–3 are solid

**Phase 5 — Build optimization + deployment**
- Apply all mandatory build settings (LTO, strip, Gzip)
- Deploy to GitHub Pages, verify `?type=invite` and `?type=home` routing
- Generate QR codes from HTTPS URL, test scan on iPhone + Android
- Memory profile on weakest target device (iPhone SE if expected)
- Test browser cache behavior: deploy → load → redeploy change → verify new version loads

**Dependencies:**
```
Phase 1 (Bootloader/App) → all other phases
Phase 3 (Apps Script) → can deploy and test independently before Phase 1 is done
Phase 2 + Phase 3 → Phase 4 (Invite scene needs Home scene to route to)
Phase 5 → last (requires shippable build)
```

**What can be safely deferred:**
- Invite scene animation (Phase 4) — the URL route can default to Home; guests still get all content
- FAQ content — placeholder text ships; couple fills it in last
- Per-group breakfast preference — `hasBreakfastPref` bool is in the model from day 1 but the UI conditional is trivial to add
- Post-wedding photo button — v2, one `Application.OpenURL()` call

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Stack | **HIGH** | All technologies fixed; patterns verified against Unity 6.0 docs (built 2026-04-16) |
| Features | **HIGH** | Wedding app patterns are mature; Italian cultural nuances flagged for couple confirmation |
| Architecture | **HIGH** | Patterns derived from existing codebase + Unity 6 documented behavior |
| RSVP / Apps Script | **HIGH** | Verified against official Apps Script docs (2026-04-01); CORS/redirect behavior confirmed |
| PlayerPrefs/WebGL | **HIGH** | Unity 6 manual + community-verified iOS private mode behavior |
| Build optimization | **HIGH** | Unity 6 Player Settings documented; Brotli/Gzip recommendation verified for GitHub Pages |
| Italian cultural norms | **MEDIUM** | "codice" vs "password" framing and registry link avoidance are reasonable but unconfirmed with couple |

**Gaps to resolve during planning:**
- Confirm with couple: registry link appropriate or not? Cash gift norm (busta) is assumed.
- Confirm target device range: if any guests have iPhone SE 1st gen or older Android, memory budget tightens further.
- Decide whether Invite scene ships in v1 or is cut for deadline. It's the only phase with no hard dependencies from other features.
- Confirm whether dietary restrictions free-text field is required — if removable, eliminates mobile keyboard layout risk entirely.
