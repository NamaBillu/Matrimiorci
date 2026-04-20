# Phase 3: RSVP — Research

**Date:** 2026-04-17
**Mode:** implementation
**Confidence:** High — all 8 domains investigated; patterns are well-established in Unity 6 + Google Apps Script ecosystem.

---

## Summary

`UnityWebRequest.Get(url)` with a coroutine is the correct and only approach for WebGL HTTP in Unity 6; there are no breaking changes from Unity 2022 LTS in this area. Google Apps Script GET requests work reliably with flat indexed URL params (`guest0Name=...&guest1Name=...`) — this is the most debuggable approach for <10 guests and avoids the URL-decoding complexity of JSON-as-param. The most common pitfall in this phase is **missing `Uri.EscapeDataString()`** on Italian text (names with accents, notes with commas/ampersands) — unencoded strings silently corrupt the Apps Script `e.parameter` values. Google Apps Script responds with `Access-Control-Allow-Origin: *` on all doGet responses, so CORS is not a concern in WebGL.

---

## Standard Stack

| Concern | Use This |
|---------|----------|
| HTTP request | `UnityWebRequest.Get(url)` — coroutine pattern |
| URL encoding | `Uri.EscapeDataString(value)` — do NOT use `WWW.EscapeURL()` (deprecated) |
| Multi-guest URL params | Flat indexed params: `guest0Name`, `guest0Attendance`, `guest0Meal`, `guest1Name`, … |
| Toggle reading | Explicit `[SerializeField] Toggle` references on `GuestRowUI` — read `.isOn` directly |
| Notes field | `TMP_InputField` with `lineType = MultiLineNewline`, `characterLimit = 300` |
| Scroll view dynamic rows | `Instantiate(prefab, container)` + `ContentSizeFitter` on content root |
| Submitted flag | `PlayerPrefs.GetInt("RSVPSubmitted", 0)` / `SetInt` + `Save()` |
| Coroutine cancel | `StopCoroutine(_submitCoroutine)` in `OnHiding()` |
| Request disposal | `request.Dispose()` in a `finally` block or `using` statement |
| Timeout | `request.timeout = 15` (seconds — int, not float) |

---

## Architecture Patterns

### 1. UnityWebRequest GET Coroutine (Unity 6 — WebGL safe)

```csharp
private Coroutine _submitCoroutine;

// Called from submit button onClick
public void OnSubmitClicked()
{
    if (_submitCoroutine != null) return; // guard double-tap
    _submitCoroutine = StartCoroutine(SubmitRSVP());
}

private IEnumerator SubmitRSVP()
{
    // Disable submit button, show spinner
    submitButton.interactable = false;
    spinnerObject.SetActive(true);
    feedbackLabel.gameObject.SetActive(false);

    string url = BuildSubmitUrl();

    using UnityWebRequest request = UnityWebRequest.Get(url);
    request.timeout = 15; // RSVP-11: 15s for venue Wi-Fi

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        // Persist flag immediately — OnApplicationQuit does not fire on WebGL tab close
        PlayerPrefs.SetInt("RSVPSubmitted", 1);
        PlayerPrefs.Save();

        ShowConfirmationPanel();
    }
    else
    {
        // Show retry message (RSVP-11)
        feedbackLabel.text = "Errore nell'invio. Riprova.";
        feedbackLabel.gameObject.SetActive(true);
        submitButton.interactable = true;
    }

    spinnerObject.SetActive(false);
    _submitCoroutine = null;
}
```

**Note:** `using` declaration (C# 8 / Unity 2021+) auto-disposes `UnityWebRequest` when the block exits. This is preferred over manual `.Dispose()` call.

**WebGL note:** `UnityWebRequest` in WebGL runs via browser `XMLHttpRequest`. The `timeout` field maps to `XHR.timeout`. The coroutine yields correctly — no threading differences vs standalone.

---

### 2. Coroutine Cancellation in OnHiding

```csharp
protected override void OnHiding()
{
    if (_submitCoroutine != null)
    {
        StopCoroutine(_submitCoroutine);
        _submitCoroutine = null;
        // Re-enable submit button in case popup is re-opened
        submitButton.interactable = true;
        spinnerObject.SetActive(false);
    }
}
```

---

### 3. URL Construction — Flat Indexed Params (Recommended)

```csharp
private string BuildSubmitUrl()
{
    var sb = new System.Text.StringBuilder();
    sb.Append(appScriptUrl);
    sb.Append("?code=");
    sb.Append(Uri.EscapeDataString(App.Instance.CurrentGroup.groupCode));

    for (int i = 0; i < _guestRows.Count; i++)
    {
        GuestRowUI row = _guestRows[i];
        sb.Append($"&guest{i}Name=");
        sb.Append(Uri.EscapeDataString(row.GuestName));
        sb.Append($"&guest{i}Attendance=");
        sb.Append(row.IsAttending ? "si" : "no");
        sb.Append($"&guest{i}Meal=");
        sb.Append(Uri.EscapeDataString(row.SelectedMeal)); // "carne"/"pesce"/"vegetariano"
    }

    if (App.Instance.CurrentGroup.hasBreakfastPref)
    {
        sb.Append("&breakfast=");
        sb.Append(Uri.EscapeDataString(GetBreakfastValue()));
    }

    sb.Append("&notes=");
    sb.Append(Uri.EscapeDataString(notesInput.text));

    return sb.ToString();
}
```

**URL length safety check:** For 6 guests × ~60 chars/guest + notes (300 chars) ≈ 800 chars total. Google Apps Script GET limit is ~2048 chars. This is safe for the <10-guest groups in this project.

---

### 4. Apps Script Side — Reading Flat Indexed Params

```javascript
// Google Apps Script doGet(e)
function doGet(e) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
  var code = e.parameter["code"];
  var notes = e.parameter["notes"] || "";
  var breakfast = e.parameter["breakfast"] || "";

  var row = [new Date(), code];
  var i = 0;
  while (e.parameter["guest" + i + "Name"]) {
    row.push(e.parameter["guest" + i + "Name"]);
    row.push(e.parameter["guest" + i + "Attendance"]);
    row.push(e.parameter["guest" + i + "Meal"]);
    i++;
  }
  row.push(breakfast);
  row.push(notes);
  sheet.appendRow(row);

  return ContentService.createTextOutput("ok").setMimeType(ContentService.MimeType.TEXT);
}
```

**Key:** Use `e.parameter["key"]` (single value) not `e.parameters["key"]` (array). Apps Script GET params are always single-valued via `e.parameter`.

**CORS:** Google Apps Script deployed as "Execute as: Me / Who has access: Anyone" always responds with `Access-Control-Allow-Origin: *`. No additional headers or Unity configuration needed.

---

### 5. Runtime Row Instantiation — Scroll View

```csharp
// RSVPPopup.cs — in OnShowing()
private List<GuestRowUI> _guestRows = new();

private void PopulateGuestRows(GroupData group)
{
    // Clear existing rows first
    foreach (Transform child in guestRowContainer)
        Destroy(child.gameObject);
    _guestRows.Clear();

    foreach (string name in group.memberNames)
    {
        GameObject rowGo = Instantiate(guestRowPrefab, guestRowContainer);
        GuestRowUI row = rowGo.GetComponent<GuestRowUI>();
        row.Initialize(name);
        _guestRows.Add(row);
    }
}
```

**RectTransform / ContentSizeFitter setup (developer does in Editor):**
- ScrollView > Viewport > Content: add `VerticalLayoutGroup` (Control Child Size: Width + Height) + `ContentSizeFitter` (Vertical Fit: Preferred Size)
- Guest row prefab root: `LayoutElement` with `Preferred Height` set to the row's design height

**No code changes needed for auto-resize** — `ContentSizeFitter` handles it automatically when rows are instantiated under a `VerticalLayoutGroup` parent.

---

### 6. Toggle Group — Reading Selected Value

```csharp
// GuestRowUI.cs — accessor used by RSVPPopup at submit time
[SerializeField] private Toggle meatToggle;
[SerializeField] private Toggle fishToggle;
[SerializeField] private Toggle vegetarianToggle;

public string SelectedMeal
{
    get
    {
        if (meatToggle.isOn)        return "carne";
        if (fishToggle.isOn)        return "pesce";
        if (vegetarianToggle.isOn)  return "vegetariano";
        return "non specificato"; // fallback — should not happen if a default is pre-selected
    }
}
```

**Default selection:** Pre-select `meatToggle` in the prefab (tick `isOn` in Inspector). This ensures `SelectedMeal` always returns a non-empty value even if the guest skips that step. Never rely on "no selection" being valid.

---

### 7. TMP_InputField Multi-Line Notes

```csharp
// Set in code (or set in Inspector — both work)
notesInput.lineType = TMP_InputField.LineType.MultiLineNewline;
notesInput.characterLimit = 300; // safe for mobile keyboard + URL length
```

**Read value at submit:**
```csharp
string notes = notesInput.text; // that's all — TMP_InputField.text includes newlines
```

**WebGL note:** On mobile WebGL, the native keyboard takes over for `TMP_InputField`. `MultiLineNewline` is respected. No special handling needed.

---

### 8. PlayerPrefs Submission Flag

```csharp
// Read (in OnShowing)
bool alreadySubmitted = PlayerPrefs.GetInt("RSVPSubmitted", 0) == 1;
if (alreadySubmitted)
{
    formPanel.SetActive(false);
    confirmationPanel.SetActive(true);
    return;
}
formPanel.SetActive(true);
confirmationPanel.SetActive(false);

// Write (on success)
PlayerPrefs.SetInt("RSVPSubmitted", 1);
PlayerPrefs.Save(); // must be immediate — WebGL tab close does not fire OnApplicationQuit
```

**Unity 6 / WebGL storage:** PlayerPrefs in WebGL uses `IndexedDB` (since Unity 2020+). `PlayerPrefs.Save()` flushes synchronously to IndexedDB. No change in API — the `.Save()` call is identical to standalone. The `IndexedDB` backend is transparent; behavior is the same as documented.

---

### 9. OnShowing() Full Pattern

```csharp
protected override void OnShowing(object[] inData)
{
    // Guard: already submitted
    if (PlayerPrefs.GetInt("RSVPSubmitted", 0) == 1)
    {
        formPanel.SetActive(false);
        confirmationPanel.SetActive(true);
        return;
    }

    formPanel.SetActive(true);
    confirmationPanel.SetActive(false);
    feedbackLabel.gameObject.SetActive(false);
    submitButton.interactable = true;

    GroupData group = App.Instance.CurrentGroup;
    if (group == null)
    {
        Logger.Log("[RSVPPopup] CurrentGroup is null — cannot populate form");
        return;
    }

    PopulateGuestRows(group);

    // Breakfast panel
    bool showBreakfast = group.hasBreakfastPref;
    breakfastPanel.SetActive(showBreakfast);

    // Notes reset
    notesInput.text = string.Empty;
}
```

---

## Don't Hand-Roll

| Thing | Use Instead |
|-------|-------------|
| URL percent-encoding | `Uri.EscapeDataString(value)` (System namespace — always available) |
| HTTP request | `UnityWebRequest.Get(url)` — do not use `WWW` class (removed in Unity 2022+) |
| Coroutine yield for network | `yield return request.SendWebRequest()` — do not poll `isDone` in a loop |
| WebRequest disposal | `using UnityWebRequest request = ...` (C# 8 using declaration) — do not manually call `.Dispose()` in finally blocks (error-prone) |
| Toggle radio behavior | Unity `ToggleGroup` component in Editor — do not write mutual-exclusion logic |
| Scroll content resize | `ContentSizeFitter` + `VerticalLayoutGroup` in Editor — do not set `sizeDelta` manually |
| IndexedDB flush | `PlayerPrefs.Save()` — do not call `Application.Quit()` or any browser JS to flush |

---

## Common Pitfalls

### 1. Missing URL encoding on Italian text ⚠️ CRITICAL
If `Uri.EscapeDataString()` is omitted, names with accents (`à`, `è`, `ò`, `ì`, `ù`) and special chars in notes (`&`, `=`, `+`, `#`) will corrupt the URL. Apps Script will silently receive wrong values. **Every URL parameter value must go through `Uri.EscapeDataString()` without exception.**

### 2. `WWW.EscapeURL()` is removed
`WWW.EscapeURL()` was deprecated and removed in Unity 2022. Using it causes a compile error. Use `Uri.EscapeDataString()` only.

### 3. `request.timeout` is an `int` not a `float`
`UnityWebRequest.timeout` is typed as `int` (seconds). Assigning `15f` causes a compile error. Use `request.timeout = 15`.

### 4. UnityWebRequest `result` vs `isNetworkError` (removed)
In Unity 2020+, `isNetworkError` and `isHttpError` are removed. Use `request.result`:
- `UnityWebRequest.Result.Success`
- `UnityWebRequest.Result.ConnectionError`
- `UnityWebRequest.Result.ProtocolError`
- `UnityWebRequest.Result.DataProcessingError`

### 5. Coroutine not stopped on popup hide
If the popup is hidden mid-submission and the coroutine is not stopped, the callback may fire on a deactivated GameObject, causing NullReferenceException or double-execution on re-open. Always call `StopCoroutine(_submitCoroutine)` in `OnHiding()` if `_submitCoroutine != null`.

### 6. `Destroy()` vs `DestroyImmediate()` for row clearing
Use `Destroy(child.gameObject)` not `DestroyImmediate`. In Unity WebGL, `DestroyImmediate` in play mode can cause unexpected layout recalculations. `Destroy` is deferred to end-of-frame, which is correct here since `Instantiate` calls follow immediately.

### 7. ContentSizeFitter not updating on Instantiate
If rows are instantiated but the scroll content does not resize, it is because `Canvas.ForceUpdateCanvases()` has not been called. This is only needed if you need to scroll programmatically (e.g., scroll to bottom). For pure display, `ContentSizeFitter` updates automatically on the next layout pass — no forced update needed.

### 8. Google Apps Script 302 redirect with POST
Confirmed locked decision: **never use POST**. Apps Script always issues a 302 redirect before executing `doPost()`, and `UnityWebRequest` follows the redirect but loses the POST body. GET requests are followed correctly with all parameters intact.

### 9. Apps Script deployment must be "Anyone" (not "Anyone with link")
The Apps Script web app **access** must be set to `"Anyone"` (not authenticated), otherwise the GET request receives a 401/302 login redirect, not a `200 ok`. This is a deployment setting in the Apps Script editor, not a code concern — but it must be verified before QA.

### 10. Double-submit guard
Without a guard, rapid tapping of the submit button starts multiple coroutines. The guard `if (_submitCoroutine != null) return;` prevents this. Also set `submitButton.interactable = false` immediately.

---

## URL Construction Decision

**Use: Flat indexed GET params**

```
?code=ABC&guest0Name=Marco&guest0Attendance=si&guest0Meal=carne&guest1Name=Sara&guest1Attendance=no&guest1Meal=pesce&breakfast=&notes=nessuna+allergia
```

**Rationale:**
- **Option A (flat indexed) — CHOSEN.** Most debuggable: readable in browser network tab, readable in Apps Script logs, no JSON parsing needed server-side. For ≤6 guests × 3 fields = 18 params + 3 meta params = ~21 params total. URL stays well under the ~2048 char practical limit for Google Apps Script.
- **Option B (JSON-as-param) — REJECTED.** JSON must be double-encoded (`Uri.EscapeDataString(JsonUtility.ToJson(...))`), and `JsonUtility` does not serialize arrays of anonymous objects — a custom serializable class is required. Added complexity for no benefit at this guest count.
- **Option C (sequential GETs) — REJECTED.** Multiple requests introduce partial-submission failure risk (first guest succeeds, second fails = corrupted data). Atomicity requires a single request.

---

## Code Examples

### GuestRowUI.cs (full component)

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuestRowUI : MonoBehaviour
{
    #region Inspector Variables

    [Tooltip("Label showing the guest's name")]
    [SerializeField] private TMP_Text nameLabel;

    [Tooltip("Toggle: guest is attending")]
    [SerializeField] private Toggle attendingToggle;

    [Tooltip("Toggle: guest is NOT attending")]
    [SerializeField] private Toggle notAttendingToggle;

    [Tooltip("Meal preference: meat")]
    [SerializeField] private Toggle meatToggle;

    [Tooltip("Meal preference: fish")]
    [SerializeField] private Toggle fishToggle;

    [Tooltip("Meal preference: vegetarian")]
    [SerializeField] private Toggle vegetarianToggle;

    #endregion

    #region Properties

    public string GuestName { get; private set; }

    public bool IsAttending => attendingToggle != null && attendingToggle.isOn;

    public string SelectedMeal
    {
        get
        {
            if (meatToggle != null && meatToggle.isOn)       return "carne";
            if (fishToggle != null && fishToggle.isOn)       return "pesce";
            if (vegetarianToggle != null && vegetarianToggle.isOn) return "vegetariano";
            return "non specificato";
        }
    }

    #endregion

    #region Public Methods

    public void Initialize(string guestName)
    {
        GuestName = guestName;
        if (nameLabel != null) nameLabel.text = guestName;
        // Default: attending = true, meal = meat
        if (attendingToggle != null)  attendingToggle.isOn = true;
        if (meatToggle != null)       meatToggle.isOn = true;
    }

    #endregion
}
```

---

### RSVPPopup.cs (skeleton)

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RSVPPopup : Popup
{
    #region Inspector Variables

    [Tooltip("URL of the deployed Google Apps Script web app")]
    [SerializeField] private string appScriptUrl;

    [Tooltip("Panel shown when form is active")]
    [SerializeField] private GameObject formPanel;

    [Tooltip("Panel shown after successful submission")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("Container Transform under the ScrollView Content for guest rows")]
    [SerializeField] private Transform guestRowContainer;

    [Tooltip("Prefab for a single guest row (must have GuestRowUI component)")]
    [SerializeField] private GameObject guestRowPrefab;

    [Tooltip("Panel shown only for apartment groups (breakfast preference)")]
    [SerializeField] private GameObject breakfastPanel;

    [Tooltip("Breakfast preference toggles (developer wires all options)")]
    [SerializeField] private List<Toggle> breakfastToggles;

    [Tooltip("Shared notes / dietary restrictions input field")]
    [SerializeField] private TMP_InputField notesInput;

    [Tooltip("Submit button — disabled during request")]
    [SerializeField] private Button submitButton;

    [Tooltip("Spinner object shown during request")]
    [SerializeField] private GameObject spinnerObject;

    [Tooltip("Feedback label for error messages")]
    [SerializeField] private TMP_Text feedbackLabel;

    #endregion

    #region Member Variables

    private List<GuestRowUI> _guestRows = new();
    private Coroutine _submitCoroutine;

    #endregion

    #region Unity Methods

    protected override void OnShowing(object[] inData)
    {
        if (PlayerPrefs.GetInt("RSVPSubmitted", 0) == 1)
        {
            formPanel.SetActive(false);
            confirmationPanel.SetActive(true);
            return;
        }

        formPanel.SetActive(true);
        confirmationPanel.SetActive(false);
        feedbackLabel.gameObject.SetActive(false);
        submitButton.interactable = true;
        spinnerObject.SetActive(false);

        GroupData group = App.Instance != null ? App.Instance.CurrentGroup : null;
        if (group == null)
        {
            Logger.Log("[RSVPPopup] CurrentGroup is null");
            return;
        }

        PopulateGuestRows(group);
        breakfastPanel.SetActive(group.hasBreakfastPref);
        notesInput.text = string.Empty;
    }

    protected override void OnHiding()
    {
        if (_submitCoroutine != null)
        {
            StopCoroutine(_submitCoroutine);
            _submitCoroutine = null;
            submitButton.interactable = true;
            spinnerObject.SetActive(false);
        }
    }

    #endregion

    #region Public Methods

    public void OnSubmitClicked()
    {
        if (_submitCoroutine != null) return;
        _submitCoroutine = StartCoroutine(SubmitRSVP());
    }

    #endregion

    #region Private Methods

    private void PopulateGuestRows(GroupData group)
    {
        foreach (Transform child in guestRowContainer)
            Destroy(child.gameObject);
        _guestRows.Clear();

        foreach (string memberName in group.memberNames)
        {
            GameObject rowGo = Instantiate(guestRowPrefab, guestRowContainer);
            GuestRowUI row = rowGo.GetComponent<GuestRowUI>();
            if (row != null)
            {
                row.Initialize(memberName);
                _guestRows.Add(row);
            }
        }
    }

    private string BuildSubmitUrl()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(appScriptUrl);
        sb.Append("?code=");
        sb.Append(Uri.EscapeDataString(App.Instance.CurrentGroup.groupCode));

        for (int i = 0; i < _guestRows.Count; i++)
        {
            GuestRowUI row = _guestRows[i];
            sb.Append($"&guest{i}Name=");
            sb.Append(Uri.EscapeDataString(row.GuestName));
            sb.Append($"&guest{i}Attendance=");
            sb.Append(row.IsAttending ? "si" : "no");
            sb.Append($"&guest{i}Meal=");
            sb.Append(Uri.EscapeDataString(row.SelectedMeal));
        }

        if (App.Instance.CurrentGroup.hasBreakfastPref)
        {
            sb.Append("&breakfast=");
            sb.Append(Uri.EscapeDataString(GetBreakfastValue()));
        }

        sb.Append("&notes=");
        sb.Append(Uri.EscapeDataString(notesInput.text));

        return sb.ToString();
    }

    private string GetBreakfastValue()
    {
        if (breakfastToggles == null || breakfastToggles.Count == 0) return string.Empty;
        // Single toggle (sì/no): return "si" or "no"
        // Multiple toggles: collect all isOn labels (developer sets toggle names to match label text)
        var selected = new System.Collections.Generic.List<string>();
        foreach (Toggle t in breakfastToggles)
        {
            if (t != null && t.isOn)
            {
                TMP_Text label = t.GetComponentInChildren<TMP_Text>();
                selected.Add(label != null ? label.text : t.name);
            }
        }
        return string.Join(",", selected);
    }

    private IEnumerator SubmitRSVP()
    {
        submitButton.interactable = false;
        spinnerObject.SetActive(true);
        feedbackLabel.gameObject.SetActive(false);

        string url = BuildSubmitUrl();

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PlayerPrefs.SetInt("RSVPSubmitted", 1);
            PlayerPrefs.Save();

            formPanel.SetActive(false);
            confirmationPanel.SetActive(true);
        }
        else
        {
            Logger.Log($"[RSVPPopup] Submit failed: {request.error}");
            feedbackLabel.text = "Errore nell'invio. Controlla la connessione e riprova.";
            feedbackLabel.gameObject.SetActive(true);
            submitButton.interactable = true;
        }

        spinnerObject.SetActive(false);
        _submitCoroutine = null;
    }

    private void ShowConfirmationPanel()
    {
        formPanel.SetActive(false);
        confirmationPanel.SetActive(true);
    }

    #endregion
}
```

---

## Verification Checklist

| Concern | How to Verify |
|---------|---------------|
| CORS | Open browser DevTools Network tab during WebGL test — Apps Script response headers must include `Access-Control-Allow-Origin: *` |
| URL encoding | Test with a guest name containing `à`, `è`, `ù` and a note with `&` — verify Google Sheet shows correct value |
| Timeout fires | Block the URL (wrong endpoint) — spinner should dismiss and error label appear after 15s |
| Double-submit guard | Rapid-tap submit button — only one network request should appear in DevTools |
| PlayerPrefs flag | Submit successfully → close tab → reopen → open RSVP popup → confirmation panel should appear (not form) |
| Row clearing | Open popup, close, re-open — guest rows should not be duplicated |
| Coroutine cancel | Start submission → close popup mid-flight → reopen → form should be in clean state, not spinner state |
| Scroll view resize | Add a 5-guest group — all rows should be visible and scroll content should auto-expand |
| Breakfast panel hide | Use a non-apartment group — breakfast panel must not be visible |
| Apps Script "Anyone" access | Make a direct browser GET to the deployed URL — should return `ok` text with no login redirect |
| `request.result` enum | Console log `request.result` on a forced failure — must be `ConnectionError` or `ProtocolError`, not a NullRef |
| Notes multi-line | Enter a multi-line note — `\n` characters should appear in the Google Sheet cell |
