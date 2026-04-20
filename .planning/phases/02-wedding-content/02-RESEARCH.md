# Phase 2: Wedding Content — Research

**Phase:** 02-wedding-content
**Date:** 2026-04-17
**Scope:** SectionButton, GroupContentGate, GroupData extension, AlloggioPopup pattern, HomeSceneController

---

## Summary

Phase 2 delivers three scripted components and one data model extension. All UI is handled by the developer in the Unity Editor — code provides the behavioral layer only. Every component extends the patterns established in Phase 1.

---

## Deliverables

| Deliverable | File | Type |
|-------------|------|------|
| GroupData extension | `Assets/App/Scripts/Managers/GroupData.cs` | Modify |
| SectionButton | `Assets/App/Scripts/UI/SectionButton.cs` | Create |
| GroupContentGate | `Assets/App/Scripts/Utils/GroupContentGate.cs` | Create |
| AlloggioPopup | `Assets/App/Scripts/Popup/AlloggioPopup.cs` | Create |

---

## 1. GroupData Extension

### Current state
`GroupData.cs` is a `[Serializable]` class with 4 fields: `password`, `groupDisplayName`, `memberNames`, `hasBreakfastPref`.

### Additions needed
```csharp
[Tooltip("True for groups with a reserved apartment.")]
public bool hasApartment = false;

[Tooltip("Apartment display name, e.g. 'Appartamento Gialli'.")]
public string apartmentName = "";

[Tooltip("Full address of the apartment.")]
public string apartmentAddress = "";

[Tooltip("Additional notes: check-in time, key pickup, etc.")]
public List<string> apartmentNotes = new List<string>();
```

### Constraints
- Must maintain #region Inspector Variables structure (CONVENTIONS.md)
- Must not break existing field serialization — add fields at the END of the region
- `hasBreakfastPref` is for RSVP (Phase 3); `hasApartment` is for Alloggio popup (Phase 2) — these are separate concepts even if the same group may have both

---

## 2. SectionButton

### Purpose
A MonoBehaviour attached to every section button in Home. It knows:
1. Whether the section requires unlock
2. Which popup to open after unlock
3. Whether to show a lock icon
4. When to switch behavior after `App.OnUnlocked` fires

### Existing pattern to mirror
`ContentGate.cs` (Phase 1) — exact `OnEnable`/`OnDisable` subscription pattern with `Refresh()` + null-check on `App.Instance`.

### Design
```csharp
public class SectionButton : MonoBehaviour
{
    // Inspector
    [SerializeField] string popupId           // popup to open when unlocked
    [SerializeField] bool requiresUnlock = true
    [SerializeField] GameObject lockIcon      // shown when locked, hidden when unlocked
    [SerializeField] TextMeshProUGUI hintLabel // optional hint text (e.g. "inserisci il tuo codice per visualizzare")

    // Gets/caches Button component
    // OnEnable: App.OnUnlocked += Refresh; Refresh()
    // OnDisable: App.OnUnlocked -= Refresh
    // Refresh(): unlocked = App.Instance != null && App.Instance.IsUnlocked
    //            if locked: lockIcon.SetActive(true), set onClick to OpenPasswordPopup
    //            if unlocked: lockIcon.SetActive(false), set onClick to OpenTargetPopup
    // OpenPasswordPopup(): PopupManager.Instance.Show("PasswordPopup")
    // OpenTargetPopup(): PopupManager.Instance.Show(popupId)
}
```

### Button listener management
`Button.onClick` is a `UnityEvent`. Cleanest approach: 
- `Button.onClick.RemoveAllListeners()` then `Button.onClick.AddListener(...)` in `Refresh()`
- Cache the `Button` component in `Awake()` via `GetComponent<Button>()`
- Guard: if `!requiresUnlock`, always open popupId regardless of lock state

### Null guards required
- `if (lockIcon != null)` before any `lockIcon.SetActive()`
- `if (hintLabel != null)` before any label text mutation
- `if (PopupManager.Instance != null)` before Show calls
- `if (App.Instance != null)` in Refresh

### Edge case: popup opens PasswordPopup, user enters correct code
When user enters correct code in PasswordPopup, `App.OnUnlocked` fires. Every `SectionButton` with `requiresUnlock = true` will call `Refresh()` and switch to opening `popupId`. This happens automatically — no special handling needed.

---

## 3. GroupContentGate

### Purpose
Reveals/hides content based on **which group is unlocked** — not just whether the app is unlocked. Covers:
- All authenticated groups (empty `visibleToPasswords`)
- Specific subset of groups (2+ passwords)
- Exactly one group (1 password)

### Existing pattern to mirror  
Identical subscription pattern to `ContentGate.cs`. Key difference: condition adds group membership check on top of `IsUnlocked`.

### Design
```csharp
public class GroupContentGate : MonoBehaviour
{
    [SerializeField] List<string> visibleToPasswords  // empty = all authenticated groups
    [SerializeField] GameObject content               // toggled active/inactive

    // OnEnable: App.OnUnlocked += Refresh; Refresh()
    // OnDisable: App.OnUnlocked -= Refresh
    // Refresh():
    //   if (!App.Instance.IsUnlocked) → content.SetActive(false); return
    //   if visibleToPasswords is empty → content.SetActive(true); return
    //   currentPwd = App.Instance.CurrentGroup.password
    //   bool match = visibleToPasswords.Any(p => p.Equals(currentPwd, OrdinalIgnoreCase))
    //   content.SetActive(match)
}
```

### StringComparison
Use `StringComparer.OrdinalIgnoreCase` or `string.Equals(p, current, OrdinalIgnoreCase)` — same case-insensitive approach as `App._groupMap`.

### Null guards
- `if (App.Instance == null)` → hide and return
- `if (App.Instance.CurrentGroup == null)` → hide and return  
- `if (content == null)` → early return

---

## 4. AlloggioPopup

### Purpose
Custom `Popup` subclass for the accommodation section. Reads `App.Instance.CurrentGroup` in `OnShowing()` to decide which UI to show:
- `hasApartment == true` → show apartment details UI; hide general suggestions UI
- `hasApartment == false` → show generic B&B/hotel suggestions; hide apartment UI

### Base class contract (from Popup.cs)
```csharp
// Must override:
public override void OnShowing(object[] inData)

// Popup.cs also declares (not virtual but callable):
public void Hide(bool cancelled)
public void Hide(bool cancelled, object[] outData)
```

`OnShowing` is called from `Popup.Show()` after the show animation starts. It's the correct place to populate UI.

### Design
```csharp
public class AlloggioPopup : Popup
{
    [SerializeField] GameObject apartmentPanel   // shown when hasApartment == true
    [SerializeField] TextMeshProUGUI apartmentNameLabel
    [SerializeField] TextMeshProUGUI apartmentAddressLabel
    [SerializeField] Transform apartmentNotesContainer  // parent for dynamically added note labels
    [SerializeField] GameObject generalSuggestionsPanel // shown when hasApartment == false

    public override void OnShowing(object[] inData)
    {
        GroupData group = App.Instance?.CurrentGroup;
        if (group == null) return;

        bool hasApt = group.hasApartment;
        apartmentPanel?.SetActive(hasApt);
        generalSuggestionsPanel?.SetActive(!hasApt);

        if (hasApt)
        {
            if (apartmentNameLabel != null) apartmentNameLabel.text = group.apartmentName;
            if (apartmentAddressLabel != null) apartmentAddressLabel.text = group.apartmentAddress;
            PopulateNotes(group.apartmentNotes);
        }
    }
}
```

### Dynamic notes
`apartmentNotes` is a `List<string>`. Use a container Transform + Instantiate pattern, or use a pre-existing TMP label and join notes with `\n`. The latter is simpler for 2–3 notes (typical use case).

Recommended: `apartmentNotesLabel.text = string.Join("\n", group.apartmentNotes)` — no prefab needed.

---

## 5. HomeSceneController (optional — thin script)

The Home scene needs nothing from code beyond what the components already provide. No `HomeSceneController` script is needed unless:
- The burger menu button needs wiring (PopupManager.Show("SideMenu"))
- A "show password popup" button exists at scene level

Both can be wired directly in the Editor via `UnityEvent → PopupManager.Show("SideMenu")` on the Button component. **No new script needed.**

---

## Architecture Fit

| Component | Extends | Subscribes to | Reads |
|-----------|---------|---------------|-------|
| `SectionButton` | `MonoBehaviour` | `App.OnUnlocked` | `App.Instance.IsUnlocked`, `PopupManager.Instance` |
| `GroupContentGate` | `MonoBehaviour` | `App.OnUnlocked` | `App.Instance.CurrentGroup.password` |
| `AlloggioPopup` | `Popup` | — | `App.Instance.CurrentGroup` in `OnShowing` |
| `GroupData` extension | `[Serializable]` class | — | — |

---

## Common Pitfalls

| Pitfall | Mitigation |
|---------|-----------|
| Button.onClick accumulates duplicate listeners across Refresh() calls | `RemoveAllListeners()` before `AddListener()` each time |
| GroupContentGate visible briefly before first Refresh | `Refresh()` called in `OnEnable` before first frame renders |
| AlloggioPopup reads CurrentGroup before unlock (null) | `if (group == null) return` guard at top of OnShowing |
| apartmentNotes list empty → joinedstring is "" → shows blank | `if (group.apartmentNotes.Count > 0)` before populating |
| `visibleToPasswords` comparison — leading/trailing whitespace | Use `p.Trim()` in the comparison or sanitize at data entry time |

---

## Files to Create/Modify

| Action | Path |
|--------|------|
| Modify | `Assets/App/Scripts/Managers/GroupData.cs` |
| Create | `Assets/App/Scripts/UI/SectionButton.cs` |
| Create | `Assets/App/Scripts/Utils/GroupContentGate.cs` |
| Create | `Assets/App/Scripts/Popup/AlloggioPopup.cs` |

**Note:** `Assets/App/Scripts/UI/` does not yet exist — create directory (Unity creates it on first file save).
