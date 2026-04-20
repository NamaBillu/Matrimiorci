# Phase 3: RSVP - Context

**Gathered:** 2026-04-17
**Status:** Ready for research and planning

<domain>
## Phase Boundary

This phase delivers the RSVP form: a password-gated popup that pre-fills group member names, collects per-guest attendance + meal + breakfast data and one shared notes field, then submits everything to Google Sheets via a GET request.

**Code provides:** behavioral architecture — popup logic, form state, submission coroutine, post-submission read-only state.
**Developer handles in Editor:** all UI layout, prefab wiring, Toggle/ToggleGroup configuration, breakfast option labels, text copy.

</domain>

<decisions>
## Implementation Decisions

### D-01: RSVPPopup Architecture

New `RSVPPopup` Popup subclass following the `AlloggioPopup` pattern (Phase 2, D-05).

- `OnShowing(object[] inData)` reads `App.Instance.CurrentGroup` to drive form population
- One per-guest row instantiated per `memberNames` entry — developer creates a guest row prefab in Editor, `RSVPPopup` holds a `[SerializeField] Transform guestRowContainer` and a `[SerializeField] GameObject guestRowPrefab`
- If `App.Instance.CurrentGroup.hasBreakfastPref == true` → show breakfast preference panel; hide it otherwise
- Checks PlayerPrefs submitted flag on `OnShowing()` → if already submitted, shows read-only confirmation panel instead of the form (see D-04)
- Developer wires all UI references (`[SerializeField]`) in Inspector; script drives show/hide and text population only

### D-02: Meal Preference UI

Per-guest meal preference uses a **Unity Toggle Group** (radio behavior — one selection at a time).

Options: `meat` / `fish` / `vegetarian`

- Developer creates the ToggleGroup with three Toggles in the guest row prefab in Editor
- `RSVPPopup` reads the selected Toggle value at submit time via the guest row's accessor
- Script exposes the three toggle references from the row: `[SerializeField] Toggle meatToggle`, `fishToggle`, `vegetarianToggle`
- At submit, read which toggle `isOn` to determine the selected meal value string (`"carne"` / `"pesce"` / `"vegetariano"`)

### D-03: Breakfast Preference

Per-group (shown only when `hasBreakfastPref == true`), multiple-choice options.

- Exact options are defined by the couple and wired in Editor (developer handles Toggle labels and prefab)
- Script exposes toggle references for all breakfast options: `[SerializeField] List<Toggle> breakfastToggles` — at submit, collects all `isOn` toggles' labels or a mapped string value
- Alternatively: if options are binary (sì/no), a single `[SerializeField] Toggle breakfastToggle` suffices — **agent's discretion** based on what the form requires; developer will wire it

### D-04: Post-Submission State

PlayerPrefs key: `"RSVPSubmitted"` (one flag per device — sufficient for <50 guests, no per-group key needed).

- On `OnShowing()`: if `PlayerPrefs.GetInt("RSVPSubmitted", 0) == 1` → **hide the form panel, show a read-only "già confermato" panel** with a confirmation message
- On submit success: set `PlayerPrefs.SetInt("RSVPSubmitted", 1); PlayerPrefs.Save();` immediately (WebGL tab-close safety)
- The form panel and confirmation panel are two sibling GameObjects under the popup — developer creates both in Editor, script toggles `SetActive()`

### D-05: Notes Field

**One shared notes field** for the whole group — not per-guest.

- Covers both RSVP-05 (dietary / allergies) and RSVP-06 (general notes) in a single `TMP_InputField` (multi-line)
- Label copy at developer's discretion in Editor (e.g., "Note e allergie")
- Script exposes: `[SerializeField] TMP_InputField notesInput`

### D-06: RSVP SectionButton Integration

`RSVPPopup` is opened via the existing `SectionButton` mechanism (Phase 2, D-02).

- The SectionButton on the Home scene has `requiresUnlock = true` and `popupId = "RSVPPopup"`
- No additional code needed — existing infrastructure handles the locked → unlocked state transition
- `PopupManager` must have `RSVPPopup` registered with ID `"RSVPPopup"` (developer wires prefab in Inspector on PopupManager)

### Agent's Discretion

- PlayerPrefs key name (exact string) — use `"RSVPSubmitted"` as defined above
- Whether to use a `GuestRowController` MonoBehaviour on each instantiated row or read fields directly from instantiated prefab transforms
- Null guards on all `App.Instance`, `PopupManager.Instance`, and `CurrentGroup` references (always apply per codebase conventions)
- Timeout value for `UnityWebRequest` — use 15 seconds per RSVP-11
- URL parameter encoding for multi-guest submission — **defer to research**: researcher must determine the correct GET param strategy for Google Apps Script with multiple guests (flat indexed params vs single JSON param vs multiple sequential GET requests)
- `[Tooltip]` attributes on all `[SerializeField]` fields — yes, follow existing codebase pattern

</decisions>

<deferred_ideas>
## Deferred Ideas

None raised during this discussion.
</deferred_ideas>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 1 & 2 Foundation
- `.planning/phases/01-foundation/01-01-SUMMARY.md` — App.CurrentGroup, App.IsUnlocked, App.OnUnlocked, PlayerPrefs.Save() pattern
- `.planning/phases/02-wedding-content/02-SUMMARY.md` — SectionButton (popupId, requiresUnlock), PopupManager.Show(), GroupData.hasBreakfastPref, AlloggioPopup pattern

### Codebase Conventions
- `.planning/codebase/CONVENTIONS.md` — #region block structure, naming conventions, [SerializeField] patterns
- `.planning/codebase/STRUCTURE.md` — Scripts/ folder organization

### Requirements
- `.planning/REQUIREMENTS.md` — RSVP-01 through RSVP-11

### Key Decisions (STATE.md)
- Google Apps Script: GET not POST — POST bodies silently dropped on 302 redirect
- PlayerPrefs.Save() immediately on every write — OnApplicationQuit does not fire on WebGL tab close
</canonical_refs>
