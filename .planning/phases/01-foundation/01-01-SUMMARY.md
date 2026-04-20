---
phase: 01-foundation
plan: 01
subsystem: auth
tags: [routing, password, playerprefs, webgl, unity]

requires: []
provides:
  - GroupData serializable model (password, groupDisplayName, memberNames, hasBreakfastPref)
  - App.TryUnlock(password) — validates code, saves to PlayerPrefs, fires OnUnlocked event
  - App.OnUnlocked static event — subscribed by ContentGate components
  - App.IsUnlocked / App.CurrentGroup — runtime unlock state
  - URL routing in App.Start() — resolves ?type=invite/home, PlayerPrefs check first
  - Session restore in App.Awake() — returning guests auto-unlocked before any scene loads
affects:
  - 01-02 (PasswordPopup calls TryUnlock; ContentGate subscribes to OnUnlocked)
  - Phase 2 (personalized section reads App.Instance.CurrentGroup)
  - Phase 3 (RSVP reads App.Instance.CurrentGroup.memberNames)

tech-stack:
  added: []
  patterns:
    - Dictionary<string, GroupData>(OrdinalIgnoreCase) for O(1) case-insensitive lookup
    - PlayerPrefs.Save() called inline after every write (WebGL — OnApplicationQuit never fires)
    - "#if UNITY_EDITOR guard in routing — Application.absoluteURL returns "" in Editor"
    - static event Action for cross-scene unlock notification (no GateManager needed)

key-files:
  created:
    - Assets/App/Scripts/Managers/GroupData.cs
  modified:
    - Assets/App/Scripts/Managers/App.cs

key-decisions:
  - "GroupData as plain [Serializable] class, not ScriptableObject — simpler at <10 groups, Inspector-editable"
  - "Password lookup uses StringComparer.OrdinalIgnoreCase — handles 'rossi' vs 'Rossi' silently"
  - "password.Trim() in TryUnlock — mobile keyboards may add trailing whitespace"
  - "PlayerPrefs check BEFORE URL param in ResolveDestination — returning guest always lands in Home"
  - "Removed dead loading screen code (mainLoadingAnimator, coroutines) — not needed for MVP routing"

patterns-established:
  - "TryUnlock pattern: validate → save to PlayerPrefs → Save() → SetGroup → fire event"
  - "Session restore pattern: PlayerPrefs.GetString in Awake → TryGetValue → SetGroup (no event fire needed, gates read IsUnlocked on OnEnable)"
  - "URL param parsing: Application.absoluteURL, split on ?, split on &, split on = with Uri.UnescapeDataString"

requirements-completed:
  - ROUT-01
  - ROUT-02
  - ROUT-03
  - PASS-02
  - PASS-03
  - PASS-05

duration: 10min
completed: 2026-04-17
---

# Phase 01, Plan 01: Foundation — Routing & Unlock Logic

**URL routing and password system wired end-to-end: `App.cs` now routes to the correct scene on load, validates passwords case-insensitively, persists unlock state to IndexedDB, and restores it on the next visit.**

## Performance

- **Duration:** ~10 min
- **Completed:** 2026-04-17
- **Tasks:** 2
- **Files modified:** 2 (created 1, modified 1)

## Accomplishments

### GroupData.cs — Serializable guest group model

New file at `Assets/App/Scripts/Managers/GroupData.cs`. Plain `[Serializable]` class (not ScriptableObject) with four fields:
- `password` — the code the couple gives each group
- `groupDisplayName` — shown in the personalized section after unlock
- `memberNames` — `List<string>` used by Phase 3 RSVP to pre-fill names
- `hasBreakfastPref` — `true` for apartment groups; gates breakfast field in RSVP

The couple populates the list in the Unity Inspector on the App GameObject in the Bootloader scene.

### App.cs — Routing, unlock system, session restore

Full replacement of `App.cs`. Key additions:

**Routing (`ResolveDestination()`):**
- `#if UNITY_EDITOR` guard routes Editor Play Mode to Home (avoids empty-URL crash)
- In WebGL builds: PlayerPrefs check first → if saved password exists → Home
- Otherwise: parse `?type=` query param → `"invite"` → Invite scene; anything else → Home

**Password system (`TryUnlock()`):**
- Dictionary built with `OrdinalIgnoreCase` in `Awake()` from Inspector list
- `password.Trim()` guards against mobile keyboard whitespace
- On match: `PlayerPrefs.SetString` + **`PlayerPrefs.Save()`** (inline, WebGL requirement) + `SetGroup()` + `OnUnlocked?.Invoke()`
- On failure: returns `false`, no side effects

**Session restore (`Awake()`):**
- `PlayerPrefs.GetString(PasswordKey)` → `_groupMap.TryGetValue` → `SetGroup()` directly (no event fire — ContentGate components read `IsUnlocked` on their own `OnEnable`)

**Removed:** Dead loading screen code (`mainLoadingAnimator`, `SceneManager_sceneLoaded`, `SafeCheckOnGameLoaded`, `OnGameFullyLoaded`) — not needed for MVP.

## Commits

- `66a4688` — feat(01-01): create GroupData serializable model
- `16b29c1` — feat(01-01): extend App.cs with routing, unlock system, session restore

## Verification

- `App.cs` contains `private const string PasswordKey = "saved_password"` ✓
- `App.cs` contains `public static event Action OnUnlocked` ✓
- `App.cs` contains `public bool IsUnlocked => CurrentGroup != null` ✓
- `App.cs` contains `PlayerPrefs.Save()` inside `TryUnlock()` ✓
- `App.cs` contains `#if UNITY_EDITOR` guard in `ResolveDestination()` ✓
- `App.cs` contains `StringComparer.OrdinalIgnoreCase` ✓
- `GroupData.cs` has all four fields ✓

## Notes for downstream phases

- **Plan 01-02:** `PasswordPopup.cs` should call `App.Instance.TryUnlock(codeInputField.text)` — returns bool
- **Plan 01-02:** `ContentGate.cs` subscribes to `App.OnUnlocked` (static event, no reference needed)
- **Phase 2:** Personalized section reads `App.Instance.CurrentGroup.groupDisplayName` and `App.Instance.CurrentGroup.memberNames`
- **Phase 2:** `App.Instance.CurrentGroup.hasBreakfastPref` gates the breakfast note for apartment groups
- **Phase 3:** RSVP form iterates `App.Instance.CurrentGroup.memberNames` to pre-fill guest rows
