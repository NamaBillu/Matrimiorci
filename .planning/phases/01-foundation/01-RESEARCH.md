# Phase 1 Research: Foundation

**Phase:** 01 — Foundation
**Requirements:** ROUT-01, ROUT-02, ROUT-03, PASS-01, PASS-02, PASS-03, PASS-04, PASS-05, GATE-01, GATE-02, GATE-03, GATE-04
**Date:** April 17, 2026
**Source:** Project ARCHITECTURE.md + STACK.md + PITFALLS.md + codebase baseline

---

## Summary

Phase 1 requires three independent deliverables, all shipped in one phase because they share a dependency chain:

1. **URL routing** — `App.cs` `Start()` reads `Application.absoluteURL`, checks PlayerPrefs, routes to the right scene
2. **Password system** — `GroupData` model, `App.cs` unlocking logic, `PasswordPopup` UI, persistence to PlayerPrefs
3. **Content gating** — `ContentGate` MonoBehaviour that listens to `App.OnUnlocked` and shows/hides content

No new external libraries needed. All code goes into existing files or new C# scripts in the existing folder structure.

---

## Deliverable 1: URL Routing

### Files to create / modify

| File | Action |
|------|--------|
| `Assets/App/Scripts/Managers/App.cs` | Add `GroupData`, `_groupMap`, `PasswordKey`, `IsUnlocked`, `CurrentGroup`, `OnUnlocked`, `TryUnlock()`, routing logic in `Start()` |

### Exact pattern (from ARCHITECTURE.md)

```csharp
// App.cs — add to top of file
using System;
using System.Collections.Generic;

public class App : SingletonComponent<App>
{
    private const string PasswordKey = "saved_password";

    [Header("Routing")]
    [SerializeField] private string inviteSceneId = "Invite";  // already exists
    [SerializeField] private string homeSceneId = "Home";       // already exists

    [Header("Group Database")]
    [SerializeField] private List<GroupData> groupDatabase = new();

    // Runtime state
    public GroupData CurrentGroup { get; private set; }
    public bool IsUnlocked => CurrentGroup != null;

    // Event — fired after successful unlock
    public static event Action OnUnlocked;

    private Dictionary<string, GroupData> _groupMap;

    protected override void Awake()
    {
        base.Awake();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        // Build O(1) lookup dictionary
        _groupMap = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groupDatabase)
            _groupMap[g.password] = g;

        // Restore unlock state if password already saved
        string saved = PlayerPrefs.GetString(PasswordKey, "");
        if (!string.IsNullOrEmpty(saved) && _groupMap.TryGetValue(saved, out var group))
            SetGroup(group);
    }

    private void Start()
    {
        SceneManager.LoadScene(ResolveDestination());
    }

    private string ResolveDestination()
    {
#if UNITY_EDITOR
        return homeSceneId;
#else
        // Returning guest → always Home
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString(PasswordKey, "")))
            return homeSceneId;

        // First-time visit → read URL param
        return GetQueryParam("type") == "invite" ? inviteSceneId : homeSceneId;
#endif
    }

    private static string GetQueryParam(string key)
    {
        string url = Application.absoluteURL;
        int qIdx = url.IndexOf('?');
        if (qIdx < 0) return "";
        string query = url.Substring(qIdx + 1);
        foreach (string part in query.Split('&'))
        {
            string[] kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return "";
    }

    public bool TryUnlock(string password)
    {
        if (_groupMap.TryGetValue(password.Trim(), out var group))
        {
            PlayerPrefs.SetString(PasswordKey, password.Trim());
            PlayerPrefs.Save(); // REQUIRED in WebGL — OnApplicationQuit never fires
            SetGroup(group);
            return true;
        }
        return false;
    }

    private void SetGroup(GroupData group)
    {
        CurrentGroup = group;
        OnUnlocked?.Invoke();
    }
}
```

### Critical rules
- **`PlayerPrefs.Save()` must be called immediately after every write** — WebGL maps PlayerPrefs to IndexedDB and `OnApplicationQuit()` is never fired in the browser. If `Save()` is omitted the code is lost on tab close.
- **PlayerPrefs check runs before URL parse** — returning guest always goes Home.
- **`#if UNITY_EDITOR` guard** — `Application.absoluteURL` returns empty string in Editor; without the guard the route always falls back to Home (which is fine but confusing during Invite scene development).
- **`StringComparer.OrdinalIgnoreCase`** — handles "rossi" vs "Rossi" case mismatch silently.
- **`password.Trim()`** — mobile keyboards sometimes add trailing space.

---

## Deliverable 2: GroupData Model

### Files to create

| File | Purpose |
|------|---------|
| `Assets/App/Scripts/Managers/GroupData.cs` | Serializable data class, Inspector-editable |

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GroupData
{
    public string password = "";
    public string groupDisplayName = "";      // e.g. "Famiglia Rossi"
    public List<string> memberNames = new();  // e.g. ["Marco", "Sofia"]
    public bool hasBreakfastPref = false;     // true for apartment-assigned groups
}
```

**No ScriptableObject** — at <10 groups, a plain `[Serializable]` class on `App` is simpler. The couple edits passwords in the Inspector directly. A ScriptableObject would add asset management overhead for zero benefit.

---

## Deliverable 3: PasswordPopup

### Files to create

| File | Purpose |
|------|---------|
| `Assets/App/Scripts/Popup/PasswordPopup.cs` | `Popup` subclass for password entry |

### Pattern — inherits from `Popup` base class

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordPopup : Popup
{
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI errorLabel;

    // Called by Submit button's OnClick
    public void OnSubmit()
    {
        string code = codeInputField.text;
        if (App.Instance.TryUnlock(code))
        {
            Hide(); // inherited from Popup base class
        }
        else
        {
            errorLabel.text = "Codice non valido. Riprova.";
            errorLabel.gameObject.SetActive(true);
        }
    }

    protected override void OnShowing()
    {
        // Clear input and error on each open
        codeInputField.text = "";
        if (errorLabel != null)
            errorLabel.gameObject.SetActive(false);
    }
}
```

### Popup wiring
- Register in `PopupManager`'s `popupInfos` list with id `"password"` in the Inspector
- Show via `PopupManager.Instance.Show("password")` — called from a "Codice" button on the Home scene
- The popup closes itself by calling `Hide()` on success (inherited from `Popup` base)
- `Hide()` in `Popup` drives the `hideAnimationStateMachine.PlaySequence(true)` automatically

### Error message copy (Italian, no hints)
- `"Codice non valido. Riprova."` — friendly, no hint about valid codes

---

## Deliverable 4: ContentGate Component

### Files to create

| File | Purpose |
|------|---------|
| `Assets/App/Scripts/Utils/ContentGate.cs` | Self-contained gate MonoBehaviour |

```csharp
using UnityEngine;

public enum GateMode { LockedVisible, Hidden }

public class ContentGate : MonoBehaviour
{
    [SerializeField] private GateMode mode = GateMode.LockedVisible;
    [SerializeField] private GameObject lockOverlay;  // semi-transparent overlay Image (LockedVisible only)
    [SerializeField] private GameObject content;      // the gated content root

    private void OnEnable()
    {
        App.OnUnlocked += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        App.OnUnlocked -= Refresh;
    }

    private void Refresh()
    {
        bool unlocked = App.Instance != null && App.Instance.IsUnlocked;

        if (mode == GateMode.Hidden)
        {
            content.SetActive(unlocked);
        }
        else // LockedVisible
        {
            if (lockOverlay != null)
                lockOverlay.SetActive(!unlocked);
        }
    }
}
```

### Key rules
- **Subscribe `OnEnable`, unsubscribe `OnDisable`** — handles scene reloads and toggled GameObjects without memory leaks
- **`Refresh()` on `OnEnable`** — gate that becomes active after an unlock already happened (e.g., popup open post-unlock) gets the correct state immediately
- **Null check on `App.Instance`** — gates may Awake before App in rare execution orders in Editor
- **Lock overlay is a sibling, not a child of content** — if it were a child, `content.SetActive(false)` would hide the overlay too
- **No GateManager** — individual self-contained components are sufficient for <20 gated elements

### Lock overlay visual approach
UGUI has no native blur. Use a semi-transparent `Image` component (UI Image with a tinted fill, ~60% alpha, sitting above the content in sibling order). Place a lock icon `Image` and a `TextMeshProUGUI` with `"Inserisci il tuo codice"` on top. No shader needed for MVP.

---

## Integration Wiring (Scene Setup)

### Bootloader scene
- `App` GameObject already exists
- Add `groupDatabase` list entries in Inspector (one per guest group — populated by couple)
- Scenes already exist: `Bootloader`, `Home`, `Invite` (confirmed by file scan)
- Verify all three scenes are in **Build Settings** (Edit → Build Settings → Scenes In Build)

### Home scene
- Add a "Codice" button that calls `PopupManager.Instance.Show("password")`
- Add `PasswordPopup` prefab to the scene, register in `PopupManager.popupInfos` with id `"password"`
- Add `ContentGate` components to sections that need locking

### No new scenes needed
All three scenes already exist in the project. Phase 1 only modifies script logic and adds prefab components.

---

## Standard Stack (confirmed, no new dependencies)

| Need | Solution | Why |
|------|----------|-----|
| URL parsing | `Application.absoluteURL` + `string.Split` | Built-in, no library needed |
| Password persistence | `PlayerPrefs.SetString` + `PlayerPrefs.Save()` | Maps to IndexedDB in WebGL |
| Password lookup | `Dictionary<string, GroupData>(OrdinalIgnoreCase)` | O(1), case-insensitive |
| Popup UI | Extend `Popup` base class | Existing pattern; `PopupManager` handles show/hide |
| Content gating | `ContentGate : MonoBehaviour` + `App.OnUnlocked` event | No central manager needed |
| Text rendering | TextMeshPro (`TMP_InputField`, `TextMeshProUGUI`) | Already in project |

---

## Don't Hand-Roll

| Pattern | Reason |
|---------|--------|
| Custom Scene loader | `SceneManager.LoadScene()` is sufficient; no async needed for Bootloader routing |
| Custom singleton | Use `SingletonComponent<T>` — already in codebase |
| Custom popup base | Use `Popup` base class — PopupManager already manages lifecycle |
| JSON serialization for groups | `[Serializable]` Inspector list is enough at <10 groups |

---

## Common Pitfalls (Phase 1 specific)

| Pitfall | Prevention |
|---------|-----------|
| `PlayerPrefs.Save()` not called | Always call `Save()` immediately after `SetString()` — never rely on `OnApplicationQuit` |
| `Application.absoluteURL` empty in Editor | Use `#if UNITY_EDITOR` guard, default to homeSceneId |
| ContentGate subscribes in `Awake` not `OnEnable` | Use `OnEnable`/`OnDisable` pair — `Awake` subscription leaks if object is disabled/re-enabled |
| Password check order reversed | PlayerPrefs check BEFORE URL param — returning guest must always land in Home |
| Overlay is child of content | Keep overlay as sibling — `content.SetActive(false)` would hide the overlay too |
| Missing `PlayerPrefs.Save()` after PASS-03 | Covered by `TryUnlock()` implementation — always call `Save()` inline |
