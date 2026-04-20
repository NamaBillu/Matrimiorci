# Architecture Patterns: Matrimiorci Unity WebGL Wedding App

**Project:** Matrimiorci
**Date:** April 17, 2026
**Codebase baseline:** SingletonComponent<T>, App, PopupManager, SoundManager, Popup

---

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│  BOOTLOADER SCENE                                           │
│  ┌──────────┐  parse URL → route to scene                  │
│  │   App    │  check PlayerPrefs → override route          │
│  └──────────┘                                               │
└─────────────────────────────────────────────────────────────┘
         │                        │
         ▼                        ▼
┌──────────────────┐    ┌─────────────────────────────────┐
│  INVITE SCENE    │    │  HOME SCENE                     │
│  If PlayerPrefs  │    │  All wedding info               │
│  has password → ─┼───▶│  PasswordPopup (Popup subclass) │
│  skip to Home    │    │  ContentGate components         │
│  Otherwise: view │    │  RSVPFormPopup (Popup subclass) │
│  invitation then │    │  Per-group personalized data    │
│  enter Home      │    └─────────────────────────────────┘
└──────────────────┘

Persistent singletons (DontDestroyOnLoad):
  App ── owns GroupDatabase, CurrentGroup, password state
  PopupManager ── show/hide all popups by string ID
  SoundManager ── music/SFX
```

---

## 1. URL Param Routing in Bootloader

### Decision
Routing lives in `App.Start()`. `App` already owns scene navigation (scene IDs are SerializeField). Bootloader is the only scene that needs routing logic — it runs exactly once.

### Pattern

```csharp
// In App.cs — Start()
private void Start()
{
    string destination = ResolveDestination();
    SceneManager.LoadScene(destination);
}

private string ResolveDestination()
{
    // 1. If password already stored → always Home
    if (!string.IsNullOrEmpty(PlayerPrefs.GetString(PasswordKey, "")))
        return homeSceneId;

    // 2. Parse ?type= query param from URL
    string urlType = GetQueryParam("type");

    return urlType == "invite" ? inviteSceneId : homeSceneId;
}

private string GetQueryParam(string key)
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
```

### Key Rules
- **PlayerPrefs check is first** — a returning guest always lands in Home regardless of URL.
- **Default to Home** — if URL has no `?type=`, `?type=home`, or any unexpected value, Home is the safe fallback.
- **No async** — `SceneManager.LoadScene()` (not `Async`) is fine here; Bootloader has a loading overlay.
- **Editor fallback** — `Application.absoluteURL` returns empty string in Editor. Add `#if UNITY_EDITOR` guard that defaults to the home scene for Play Mode testing.

```csharp
private string ResolveDestination()
{
#if UNITY_EDITOR
    return homeSceneId; // or inviteSceneId while building Invite
#endif
    if (!string.IsNullOrEmpty(PlayerPrefs.GetString(PasswordKey, "")))
        return homeSceneId;

    return GetQueryParam("type") == "invite" ? inviteSceneId : homeSceneId;
}
```

### Component Diagram

```
Bootloader Scene
└── [App GameObject] (DontDestroyOnLoad)
    └── App.Start()
        ├── PlayerPrefs.GetString(PasswordKey) → if set → homeSceneId
        ├── Application.absoluteURL → parse ?type= → inviteSceneId / homeSceneId
        └── SceneManager.LoadScene(destination)
```

---

## 2. Password / Group Data Architecture

### Decision
**Plain C# `[Serializable]` class in `App` Inspector** — not ScriptableObject. At <10 groups a ScriptableObject adds asset management overhead with zero benefit. The list is editable in the Inspector, serialized into the scene, and converted to a Dictionary at runtime.

### GroupData Model

```csharp
[System.Serializable]
public class GroupData
{
    public string password = "";
    public string groupDisplayName = "";         // "Famiglia Rossi"
    public List<string> memberNames = new();     // ["Marco", "Sofia"]
    public bool hasBreakfastPref = false;        // apartment guests only
    // Add per-group fields here as requirements expand
}
```

### App.cs additions

```csharp
public class App : SingletonComponent<App>
{
    private const string PasswordKey = "saved_password";

    [Header("Group Database")]
    [SerializeField] private List<GroupData> groupDatabase = new();

    // Runtime state — set after password validation
    public GroupData CurrentGroup { get; private set; }
    public bool IsUnlocked => CurrentGroup != null;

    // Event — fired after successful unlock
    public static event Action OnUnlocked;

    // Built once in Awake for O(1) lookup
    private Dictionary<string, GroupData> _groupMap;

    protected override void Awake()
    {
        base.Awake();
        _groupMap = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groupDatabase)
            _groupMap[g.password] = g;

        // Restore session if password was already stored
        string saved = PlayerPrefs.GetString(PasswordKey, "");
        if (!string.IsNullOrEmpty(saved) && _groupMap.TryGetValue(saved, out var group))
            SetGroup(group);
    }

    public bool TryUnlock(string password)
    {
        if (_groupMap.TryGetValue(password.Trim(), out var group))
        {
            PlayerPrefs.SetString(PasswordKey, password.Trim());
            PlayerPrefs.Save();
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

### Key Rules
- **`StringComparer.OrdinalIgnoreCase`** — avoids "rossi" vs "Rossi" failures.
- **`password.Trim()`** — avoids leading/trailing space failures from mobile keyboards.
- **Restore on Awake** — if PlayerPrefs has a valid password, `CurrentGroup` is populated before any scene loads. Content gates that check `App.Instance.IsUnlocked` on Start() get the right state.
- **`OnUnlocked` is a static event** — subscribers don't need an App reference. Unsubscribe in `OnDisable`.
- **ScriptableObject is NOT recommended** — it requires an asset file, a reference on App, and adds nothing when passwords are known at ship time.

---

## 3. Content Gating Pattern

### Two Gate Modes

| Mode | UGUI Technique | User sees |
|------|---------------|-----------|
| **Locked** (visible, blurred) | Overlay panel + CanvasGroup | Content visible but covered |
| **Hidden** (not visible) | `gameObject.SetActive(false)` or `CanvasGroup.alpha = 0` | Nothing |

### Locked Gate — Overlay Approach
Unity UGUI has no native blur. Use a semi-transparent overlay `Image` that sits on top of the content (same parent, higher sibling index). The `ContentGate` component drives this.

```
[Content Panel]  ← holds the gated content
[Lock Overlay]   ← Image with tinted/frosted appearance, sits above content
```

No shader needed — an `Image` with a frosted glass sprite (or a solid fill at ~60% alpha) reads as "locked" visually.

### ContentGate Component

```csharp
public enum GateMode { LockedVisible, Hidden }

public class ContentGate : MonoBehaviour
{
    [SerializeField] private GateMode mode = GateMode.LockedVisible;
    [SerializeField] private GameObject lockOverlay;   // only for LockedVisible
    [SerializeField] private GameObject content;       // the gated content root

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

### Key Rules
- **No GateManager singleton** — individual `ContentGate` components are self-contained. With <20 gated elements on screen, a central manager adds complexity without benefit.
- **Subscribe in `OnEnable`, unsubscribe in `OnDisable`** — handles scene reloads and objects that are toggled on/off without memory leaks.
- **`Refresh()` called on `OnEnable`** — handles the case where a gate becomes active after the password was already entered (e.g., a popup that opens post-unlock).
- **Null check on `App.Instance`** — gates may awaken before App in rare execution orders.
- **Lock overlay is a sibling, not a child** — placing it as a child of `content` means `content.SetActive(false)` would also hide the overlay; keep them siblings.

### Component Diagram

```
[Home Scene Canvas]
├── [SectionPanel]
│   ├── [ContentRoot]          ← gated content lives here
│   │   ├── ... wedding info
│   └── [LockOverlay]          ← Image, sits above ContentRoot
│   └── ContentGate.cs         ← on SectionPanel, refs both above
└── [HiddenSection]
    ├── [RSVPSection]          ← gated content
    └── ContentGate.cs (mode=Hidden)
```

---

## 4. RSVP Form Architecture

### Decision
`RSVPFormPopup : Popup` — reuse the existing Popup base class and PopupManager infrastructure. The popup reads `App.Instance.CurrentGroup` directly — no data passing through `object[]` inData needed because there's exactly one active group at a time.

### Data Model (extends GroupData)

```csharp
[System.Serializable]
public class GroupData
{
    public string password = "";
    public string groupDisplayName = "";
    public List<string> memberNames = new();
    public bool hasBreakfastPref = false;
}

// Collected from the form at submission time
public class RSVPSubmission
{
    public string groupName;
    public List<MemberRSVP> members;
    public string notes;
    public string breakfastPref; // null if hasBreakfastPref == false
    public string timestamp;
}

public class MemberRSVP
{
    public string name;
    public bool isAttending;
    public string mealPref; // optional per member
}
```

### RSVPFormPopup

```csharp
public class RSVPFormPopup : Popup
{
    [Header("Member Row Template")]
    [SerializeField] private RSVPMemberRow memberRowPrefab;
    [SerializeField] private Transform memberRowContainer;

    [Header("Optional Fields")]
    [SerializeField] private GameObject breakfastPrefSection;
    [SerializeField] private TMP_Dropdown breakfastPrefDropdown;

    [Header("Common Fields")]
    [SerializeField] private TMP_InputField notesField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject errorPanel;

    private List<RSVPMemberRow> _activeRows = new();

    public override void Initialize()
    {
        base.Initialize();
        // Member rows are instantiated on Show, not here
    }

    protected override void OnShow(object[] inData)
    {
        GroupData group = App.Instance.CurrentGroup;
        BuildMemberRows(group);
        breakfastPrefSection.SetActive(group.hasBreakfastPref);
        SetFormState(FormState.Input);
    }

    private void BuildMemberRows(GroupData group)
    {
        // Clear existing rows
        foreach (var row in _activeRows)
            Destroy(row.gameObject);
        _activeRows.Clear();

        // Instantiate one row per member
        foreach (string name in group.memberNames)
        {
            var row = Instantiate(memberRowPrefab, memberRowContainer);
            row.Setup(name);
            _activeRows.Add(row);
        }
    }

    public void OnSubmitClicked()
    {
        RSVPSubmission data = CollectFormData();
        StartCoroutine(SubmitRSVP(data));
    }

    private RSVPSubmission CollectFormData()
    {
        var members = new List<MemberRSVP>();
        foreach (var row in _activeRows)
            members.Add(row.GetData());

        return new RSVPSubmission
        {
            groupName = App.Instance.CurrentGroup.groupDisplayName,
            members = members,
            notes = notesField.text,
            breakfastPref = App.Instance.CurrentGroup.hasBreakfastPref
                ? breakfastPrefDropdown.options[breakfastPrefDropdown.value].text
                : null,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
```

### RSVPMemberRow

```csharp
public class RSVPMemberRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Toggle attendingToggle;
    [SerializeField] private Toggle notAttendingToggle;

    public void Setup(string memberName)
    {
        nameLabel.text = memberName;
        attendingToggle.isOn = false;
        notAttendingToggle.isOn = false;
    }

    public MemberRSVP GetData() => new MemberRSVP
    {
        name = nameLabel.text,
        isAttending = attendingToggle.isOn
    };
}
```

### Key Rules
- **Read from `App.Instance.CurrentGroup` on Show** — no data coupling through `inData`. Password entry always precedes RSVP so `CurrentGroup` is guaranteed non-null when this popup opens.
- **Instantiate rows on Show, destroy on Hide** — simpler than pooling at this scale (<10 members max). If rows are reused across Show/Hide cycles, clear and rebuild.
- **No Popup.Show() override with inData** — the existing `Show(string id)` path in PopupManager is sufficient. Don't fight the existing API.
- **Form states drive visibility** — use an enum `FormState { Input, Loading, Success, Error }` to toggle which panel is active. Avoids scattered `SetActive` calls.

```csharp
private enum FormState { Input, Loading, Success, Error }

private void SetFormState(FormState s)
{
    submitButton.interactable = s == FormState.Input;
    loadingIndicator.SetActive(s == FormState.Loading);
    successPanel.SetActive(s == FormState.Success);
    errorPanel.SetActive(s == FormState.Error);
    // memberRowContainer and fields stay visible in all states
}
```

---

## 5. Google Sheets Submission Pattern

### Decision
Coroutine directly in `RSVPFormPopup`. No dedicated `RSVPManager` — one call site, one responsibility. A manager would only be justified if multiple popups submitted RSVP data (they don't).

### Submission Coroutine

```csharp
private const string AppsScriptUrl = "https://script.google.com/macros/s/YOUR_SCRIPT_ID/exec";

private IEnumerator SubmitRSVP(RSVPSubmission data)
{
    SetFormState(FormState.Loading);

    string json = JsonUtility.ToJson(data);
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

    using var request = new UnityWebRequest(AppsScriptUrl, "POST");
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        SetFormState(FormState.Success);
        // Mark as submitted in PlayerPrefs so they can't double-submit
        PlayerPrefs.SetInt(SubmittedKey, 1);
        PlayerPrefs.Save();
    }
    else
    {
        Debug.LogWarning($"[RSVP] Submission failed: {request.error}");
        SetFormState(FormState.Error);
    }
}
```

### Apps Script Side (reference)

The Apps Script endpoint must handle POST with a JSON body and append to a sheet:

```javascript
// Google Apps Script — doPost(e)
function doPost(e) {
  var data = JSON.parse(e.postData.contents);
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName("RSVPs");
  
  data.members.forEach(function(m) {
    sheet.appendRow([
      data.timestamp,
      data.groupName,
      m.name,
      m.isAttending ? "Sì" : "No",
      data.breakfastPref || "",
      data.notes || ""
    ]);
  });
  
  return ContentService.createTextOutput("ok");
}
```

**Critical:** Apps Script web app must be deployed as "Anyone can access" (anonymous POST). CORS is handled by Google's infrastructure.

### Preventing Double Submission

```csharp
private const string SubmittedKey = "rsvp_submitted";

protected override void OnShow(object[] inData)
{
    bool alreadySubmitted = PlayerPrefs.GetInt(SubmittedKey, 0) == 1;
    if (alreadySubmitted)
    {
        SetFormState(FormState.Success); // show success state immediately
        return;
    }
    // ... normal show flow
}
```

### Key Rules
- **`using var request`** — UnityWebRequest implements IDisposable; always dispose to avoid resource leaks.
- **Disable submit button on first click** — `SetFormState(FormState.Loading)` handles this. Never rely on the user not double-tapping.
- **No retry loop** — show the error panel with a "Riprova" button that calls `OnSubmitClicked()` again. Simple and explicit.
- **`JsonUtility.ToJson`** — built-in, no external JSON lib needed. Works with `[Serializable]` classes. **Caveat:** `JsonUtility` does not serialize `null` strings as JSON null — use `""` as the default value for optional fields.
- **`SubmittedKey` in PlayerPrefs** — prevents double submission across page reloads. Clear it only if you implement an "Edit RSVP" feature (deferred).

---

## Component Ownership Map

```
App (SingletonComponent, DontDestroyOnLoad)
├── GroupDatabase            List<GroupData>  [Inspector]
├── CurrentGroup             GroupData        [runtime]
├── IsUnlocked               bool             [derived]
├── OnUnlocked               static event     [broadcast]
├── TryUnlock(password)      method
└── GetQueryParam(key)       method

PopupManager (SingletonComponent, DontDestroyOnLoad)
├── PasswordPopup : Popup    id = "password"
└── RSVPFormPopup : Popup    id = "rsvp"

Home Scene
├── ContentGate[]            read App.IsUnlocked, subscribe OnUnlocked
├── PersonalizedSection      read App.CurrentGroup on enable
└── [Open Password Popup Button] → PopupManager.Show("password")

Invite Scene
├── [View Invitation UI]
└── [Enter Home Button] → SceneManager.LoadScene("Home")
```

---

## PasswordPopup Pattern

```csharp
public class PasswordPopup : Popup
{
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text errorLabel;
    [SerializeField] private Button confirmButton;

    protected override void OnShow(object[] inData)
    {
        passwordInput.text = "";
        errorLabel.gameObject.SetActive(false);
        passwordInput.ActivateInputField(); // auto-focus keyboard
    }

    public void OnConfirmClicked()
    {
        string pwd = passwordInput.text.Trim();
        if (App.Instance.TryUnlock(pwd))
        {
            Hide(); // base Popup.Hide()
        }
        else
        {
            errorLabel.gameObject.SetActive(true);
            errorLabel.text = "Password non valida";
            passwordInput.text = "";
        }
    }
}
```

**Note:** `PasswordInput` should use `contentType = Password` in the Inspector for character masking. No hashing needed — this is a convenience gate, not security.

---

## PersonalizedSection Pattern

For sections that display group-specific data (names, guest count):

```csharp
public class PersonalizedSection : MonoBehaviour
{
    [SerializeField] private TMP_Text welcomeLabel;
    [SerializeField] private GameObject content; // shown only when unlocked

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
        if (App.Instance == null || !App.Instance.IsUnlocked)
        {
            content.SetActive(false);
            return;
        }
        content.SetActive(true);
        welcomeLabel.text = $"Benvenuti, {App.Instance.CurrentGroup.groupDisplayName}!";
    }
}
```

---

## Build Order (Implementation Phases)

Ordered by dependency — each phase unblocks the next.

| Order | Component | Depends On | Notes |
|-------|-----------|------------|-------|
| 1 | `GroupData` model | nothing | Add to App.cs or own file |
| 2 | `App.TryUnlock()` + `OnUnlocked` event | GroupData | Core unlock path |
| 3 | URL routing in `App.Start()` | App.Awake() (Awake restores PlayerPrefs) | Bootloader routing |
| 4 | `ContentGate` component | App.IsUnlocked, App.OnUnlocked | Attach to Home scene objects |
| 5 | `PasswordPopup` | App.TryUnlock(), Popup base | Register in PopupManager |
| 6 | `PersonalizedSection` | App.CurrentGroup, App.OnUnlocked | After ContentGate works |
| 7 | `RSVPMemberRow` prefab | nothing | Pure UI prefab |
| 8 | `RSVPFormPopup` | GroupData, RSVPMemberRow, Popup base | Depends on 1–7 |
| 9 | RSVP submission coroutine | RSVPFormPopup, Apps Script deployed | Last — needs backend URL |
| 10 | Invite scene flow | App routing, App.IsUnlocked | Skip-to-Home logic |

---

## Pitfalls to Avoid

| Pitfall | Problem | Fix |
|---------|---------|-----|
| Checking `App.Instance.IsUnlocked` in `Start()` before App.Awake() | Race condition on first load | Use `OnEnable` + subscribe to `OnUnlocked` |
| Forgetting to unsubscribe static events | Memory leak, null ref after scene unload | Always pair `+=` with `-=` in `OnEnable`/`OnDisable` |
| `JsonUtility.ToJson` on List | `JsonUtility` cannot serialize a top-level `List<T>` | Wrap in a class: `[Serializable] class Wrapper { public List<T> items; }` |
| `Application.absoluteURL` in Editor | Returns empty string | `#if UNITY_EDITOR` guard with hardcoded destination |
| Apps Script CORS on WebGL | WebGL requests from `localhost` or itch.io may be blocked | Deploy Apps Script as "Anyone", test from the live domain |
| Double RSVP submission on slow networks | User taps submit twice | Disable button immediately in `SetFormState(Loading)` |
| PlayerPrefs `PasswordKey` collision with `SubmittedKey` | If using same key name | Use distinct constant strings; prefix with app name |

---

## Sources & Confidence

| Area | Confidence | Basis |
|------|------------|-------|
| URL routing pattern | HIGH | Unity docs: `Application.absoluteURL` is available in WebGL builds; `Uri` parsing is standard C# |
| GroupData in Inspector list | HIGH | Standard Unity serialization pattern; confirmed with existing SoundManager.SoundInfo pattern in codebase |
| ContentGate with static event | HIGH | Matches existing App/PopupManager patterns; OnEnable/OnDisable subscription is idiomatic Unity |
| RSVPFormPopup extending Popup | HIGH | Directly extends existing Popup base class and PopupManager registration |
| UnityWebRequest POST to Apps Script | HIGH | Standard Unity networking; Apps Script CORS behavior is well-documented |
| Blur via overlay (not shader) | MEDIUM | Native UGUI blur requires shader; overlay is the pragmatic alternative within 2-week timeline |
| `JsonUtility` for submission | HIGH | Built-in, no dependencies; `[Serializable]` classes required |
