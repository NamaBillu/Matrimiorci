# Technology Stack — Unity 6 WebGL Wedding App

**Project:** Matrimiorci — Unity 6 (6000.3.5f2) WebGL
**Researched:** 2026-04-17
**Sources:** Unity 6.0 Manual (built 2026-04-16), Google Apps Script docs (updated 2026-04-01), community patterns

---

## Recommended Stack

| Technology | Version | Purpose | Status |
|------------|---------|---------|--------|
| Unity | 6000.3.5f2 | Runtime, build target | Existing — fixed |
| C# / IL2CPP | .NET Standard 2.1 | Scripting | Existing — fixed |
| UGUI | Built-in | UI system | Existing — fixed |
| DOTween | Latest | Tweening animations | Existing — fixed |
| UIAnimations (NamaBillu) | Current | State machine animations | Existing — fixed |
| Google Apps Script | N/A | Free RSVP HTTP endpoint writing to Sheets | **Recommended** |
| GitHub Pages | N/A | Hosting (free, direct URL control) | **Recommended** |
| Browser localStorage / IndexedDB | N/A | Password persistence via PlayerPrefs | Built-in to WebGL |

---

## Question 1: URL Query Param Parsing in Unity 6 WebGL

### Confidence: HIGH (verified against Unity 6.0 docs + known behavior)

### How It Works

`Application.absoluteURL` returns the full browser URL at runtime, including query string.
Only available in WebGL builds — returns empty string in Editor.

### Pattern A — Pure C# (Recommended for GitHub Pages hosting)

```csharp
// Works on GitHub Pages and any direct-URL host.
// Application.absoluteURL example: "https://yourname.github.io/wedding/?type=invite"

public static string GetQueryParam(string key)
{
    string url = Application.absoluteURL;
    if (!url.Contains("?")) return "";

    string query = url.Substring(url.IndexOf('?') + 1);
    // Strip fragment if present
    if (query.Contains("#")) query = query.Substring(0, query.IndexOf('#'));

    foreach (string param in query.Split('&'))
    {
        string[] kv = param.Split('=');
        if (kv.Length == 2 && Uri.UnescapeDataString(kv[0]) == key)
            return Uri.UnescapeDataString(kv[1]);
    }
    return "";
}

// In Bootloader Awake():
#if UNITY_WEBGL && !UNITY_EDITOR
    string routeType = GetQueryParam("type"); // "home" | "invite" | ""
#else
    string routeType = "home"; // default for Editor testing
#endif
```

### Pattern B — jslib Bridge (Recommended if iframe embedding is needed)

When Unity is embedded in an `<iframe>`, `Application.absoluteURL` returns the iframe's `src` URL, NOT the outer page URL. On itch.io, the iframe src is a CDN URL with no guest-supplied query params.

If you need iframe support, use a `.jslib` plugin to read from JavaScript directly.

**File: `Assets/Plugins/WebBridge.jslib`**

```javascript
mergeInto(LibraryManager.library, {

    GetURLQueryString: function() {
        var qs = window.location.search || "";
        var bufferSize = lengthBytesUTF8(qs) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(qs, buffer, bufferSize);
        return buffer;
    },

    GetURLHash: function() {
        var hash = window.location.hash || "";
        var bufferSize = lengthBytesUTF8(hash) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(hash, buffer, bufferSize);
        return buffer;
    }

});
```

**File: `Assets/Scripts/WebBridge.cs`**

```csharp
using System.Runtime.InteropServices;
using UnityEngine;

public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetURLQueryString();

    [DllImport("__Internal")]
    private static extern string GetURLHash();
#endif

    public static string QueryParam(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string qs = GetURLQueryString(); // e.g. "?type=invite"
#else
        string qs = "?type=home";        // Editor default
#endif
        if (string.IsNullOrEmpty(qs) || !qs.Contains("?")) return "";
        string query = qs.TrimStart('?');
        foreach (string part in query.Split('&'))
        {
            string[] kv = part.Split('=');
            if (kv.Length == 2 && Uri.UnescapeDataString(kv[0]) == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return "";
    }
}
```

> **Unity 6 jslib note:** Unity 6 enables `WebAssembly.Table` by default for new projects. If you use `_malloc` / `stringToUTF8` in a `.jslib`, these are Emscripten runtime functions and remain available. The old `dynCall_` family is deprecated — avoid it. The `.jslib` runs in the same scope as the compiled build, so `_malloc`, `stringToUTF8`, `lengthBytesUTF8` are all directly accessible.

### Iframe / Hosting Gotchas

| Host | `Application.absoluteURL` behavior | Recommendation |
|------|-----------------------------------|----------------|
| **GitHub Pages** | Returns full page URL with query string — works perfectly | **Use this** |
| **itch.io** | Returns the Unity build CDN iframe src URL (no user query params) | Avoid for URL routing; use GitHub Pages instead |
| **Netlify / Vercel** | Same as GitHub Pages — full URL returned | Also fine |
| **Local (Editor)** | Returns empty string | Gate with `#if UNITY_WEBGL && !UNITY_EDITOR` |

**Decision:** Host on GitHub Pages. `Application.absoluteURL` + Pattern A is sufficient, no jslib needed. The jslib is a fallback if itch.io is required later.

---

## Question 2: Google Sheets + Apps Script as Free HTTP Endpoint

### Confidence: HIGH (verified against official Apps Script docs 2026-04-01 + known CORS patterns)

### How Apps Script Web Apps Work

A deployed Apps Script web app at `https://script.google.com/macros/s/<ID>/exec`:
1. Receives requests via `doGet(e)` or `doPost(e)` functions
2. Is fully HTTPS — always
3. Redirects (302) to `script.googleusercontent.com` for the actual response
4. Returns CORS header `Access-Control-Allow-Origin: *` when deployed to "Anyone, even anonymous"

### Critical CORS Issue: POST + Redirect = Body Loss

When you HTTP POST to the exec URL, the server returns a **302 redirect** to googleusercontent.com. Browser's Fetch API (which Unity WebGL's `UnityWebRequest` uses internally) converts POST→GET on a 302 redirect (per HTTP spec). The POST body is lost on the redirect.

**Do NOT use POST with Apps Script from Unity WebGL.** Use GET with query params instead.

### ✅ Working Pattern: GET Request + Query Params

Apps Script parses GET params via `e.parameter`. RSVP payloads for this app are tiny (<300 chars), well within the 2KB URL length limit.

**Apps Script Code (`script.google.com`):**

```javascript
const SHEET_ID = "YOUR_GOOGLE_SHEET_ID_HERE";
const SHEET_NAME = "RSVPs";

function doGet(e) {
  try {
    var sheet = SpreadsheetApp.openById(SHEET_ID).getSheetByName(SHEET_NAME);
    
    var timestamp     = new Date().toISOString();
    var group         = e.parameter.group         || "";
    var attending     = e.parameter.attending     || "";
    var guestNames    = e.parameter.names         || "";
    var mealPref      = e.parameter.meal          || "";
    var dietary       = e.parameter.dietary       || "";
    var notes         = e.parameter.notes         || "";
    var breakfast     = e.parameter.breakfast     || ""; // apartment guests only
    
    sheet.appendRow([timestamp, group, attending, guestNames, mealPref, dietary, notes, breakfast]);
    
    return ContentService
      .createTextOutput(JSON.stringify({ status: "ok" }))
      .setMimeType(ContentService.MimeType.JSON);
      
  } catch(err) {
    return ContentService
      .createTextOutput(JSON.stringify({ status: "error", message: err.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}
```

**Unity C# Code:**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RsvpManager : MonoBehaviour
{
    private const string APPS_SCRIPT_URL = "https://script.google.com/macros/s/YOUR_DEPLOYMENT_ID/exec";

    public IEnumerator SubmitRsvp(RsvpData data)
    {
        // Build query string — no POST body, avoids redirect body-loss issue
        string url = APPS_SCRIPT_URL
            + "?group="     + UnityWebRequest.EscapeURL(data.group)
            + "&attending="  + UnityWebRequest.EscapeURL(data.attending)
            + "&names="      + UnityWebRequest.EscapeURL(data.guestNames)
            + "&meal="       + UnityWebRequest.EscapeURL(data.mealPref)
            + "&dietary="    + UnityWebRequest.EscapeURL(data.dietary)
            + "&notes="      + UnityWebRequest.EscapeURL(data.notes)
            + "&breakfast="  + UnityWebRequest.EscapeURL(data.breakfast);

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("RSVP submitted: " + req.downloadHandler.text);
        }
        else
        {
            Debug.LogWarning("RSVP failed: " + req.error);
            // TODO: Show retry UI or cache submission for retry
        }
    }
}
```

### Deployment Steps

1. Go to [script.google.com](https://script.google.com) → New project
2. Paste the `doGet` code above, update `SHEET_ID`
3. Click **Deploy → New deployment**
4. Type: **Web app**
5. Execute as: **Me (your Google account)**
6. Who has access: **Anyone, even anonymous** ← critical for no-auth access
7. Click **Deploy** → copy the `/exec` URL
8. In your Google Sheet, create a tab named "RSVPs" with headers:
   `Timestamp | Group | Attending | Names | Meal | Dietary | Notes | Breakfast`

> **Redeployment:** After any code change, you MUST create a **new deployment version** (not just save the script). The `/exec` URL stays the same after redeployment.

### CORS Explanation

- GET requests to Apps Script exec URL are "simple" CORS requests — no preflight needed
- Apps Script with "Anyone, even anonymous" returns `Access-Control-Allow-Origin: *` on the final redirected response
- Unity's `UnityWebRequest` follows redirects by default and correctly reads the CORS-enabled response
- GET → redirect (302) → GET on googleusercontent.com → CORS headers present → ✅ works

### Free Tier Limits

| Metric | Consumer (gmail.com) | Limit for this project |
|--------|---------------------|----------------------|
| Script executions | No explicit limit listed | ~50 submissions total — irrelevant |
| URL Fetch calls (outbound from script) | 20,000 / day | Not used (we write to Sheets directly) |
| Simultaneous executions | 30 / user | Not a concern at <50 guests |
| Script runtime | 6 min / execution | RSVP write takes <1 second |
| Spreadsheets written | 250 / day | Fine for 50 submissions |

**Verdict: Free tier is more than sufficient. Google Sheets + Apps Script is zero-cost forever at this scale.**

---

## Question 3: PlayerPrefs in Unity WebGL

### Confidence: HIGH (Unity behavior well-documented; iOS caveats are MEDIUM — community-verified)

### How It Works in Unity 6 WebGL

`PlayerPrefs` in WebGL maps to **browser IndexedDB** (not localStorage — that was Unity 5.x and earlier). Data is stored in a Unity-specific IndexedDB database, namespaced per build origin.

```csharp
// Writing — exactly the same API as any platform
PlayerPrefs.SetString("password_group", "groupA");
PlayerPrefs.SetInt("auth_unlocked", 1);
PlayerPrefs.Save(); // ← REQUIRED in WebGL — flushes to IndexedDB immediately

// Reading
string storedGroup = PlayerPrefs.GetString("password_group", "");
bool isUnlocked = PlayerPrefs.GetInt("auth_unlocked", 0) == 1;
```

> **`PlayerPrefs.Save()` is mandatory on WebGL.** On native platforms, data is auto-saved. On WebGL, if you don't call `Save()`, data is held in memory and will be lost if the tab is closed before the async IndexedDB write completes.

### Persistence Behavior

| Browser / Context | Persistence |
|-------------------|-------------|
| Chrome / Firefox / Edge (normal) | Persists indefinitely across sessions |
| Safari (normal mode) | Persists, but may be cleared after 7 days of no site visits (ITP — Intelligent Tracking Prevention) |
| Safari (Private / Incognito) | **Cleared on tab close** — IndexedDB is in-memory in private mode |
| Chrome Incognito | Cleared when incognito session ends |
| Any browser — "Clear site data" | Cleared |

### iOS Safari Private Mode — The Critical Gotcha

If a guest opens the link in iOS Safari private mode, their password entry will not persist after they close the tab. They'll need to re-enter it every session.

**Mitigation options:**
1. Do nothing — acceptable given the use case (wedding app, not security-critical). Guests who use private mode will re-enter the password; a 4-character wedding password is trivial.
2. Add a note in the UI: "Use normal (non-private) browsing to save your access."
3. Detect private mode and show a warning (complex, not worth it for this scale).

**Recommended:** Option 1. The password is simple and re-entry is a minor inconvenience.

### Relevant Key Names

```csharp
// Suggested constants for the Bootloader/password system
public static class PrefsKeys
{
    public const string PasswordGroup = "wedding_group";    // string — group identifier
    public const string IsUnlocked    = "wedding_unlocked"; // int    — 1 = unlocked
    // Do NOT store the actual password — just the group identifier post-validation
}
```

---

## Question 4: Unity WebGL Mobile Browser Performance

### Confidence: HIGH (from Unity 6.0 Player Settings docs, built 2026-04-16)

### Player Settings — Recommended Configuration

**Publishing Settings (File → Build Profiles → Player Settings → Publishing Settings):**

| Setting | Value | Rationale |
|---------|-------|-----------|
| Compression Format | **Brotli** (GitHub Pages) or **Gzip** (fallback) | See hosting note below |
| Data Caching | **Enabled** | Caches `.data` file in IndexedDB; returning guests skip re-download |
| Decompression Fallback | **Enabled** | Safety net if server doesn't serve correct Content-Encoding header |

**Compression by host:**
- **GitHub Pages**: Serves pre-compressed Brotli files if you include them. Requires adding a `_headers` file or configuring via custom GitHub Actions workflow. If not configured, Unity's decompression fallback handles it client-side (slower first load). **Recommended: Gzip for GitHub Pages** (works out of the box with standard serve config).
- **Itch.io**: Manages its own compression pipeline; set Unity to Disabled and let itch.io handle it.

**WebAssembly Language Features:**

| Setting | Value | Rationale |
|---------|-------|-----------|
| Enable WebAssembly 2023 | **Enabled** | SIMD, bulk memory, native exceptions — significant perf gain on modern browsers |
| BigInt | **Enabled** | Faster builds, required by Safari 14.5+, Chrome 85+, Firefox 78+ |
| Enable Native C/C++ Multithreading | **Disabled** | Requires COOP/COEP headers; not needed for a UGUI app |
| Exceptions | **Explicitly Thrown Only** | Balance of debugging ability and performance. Use None for final prod if confident. |
| Memory Growth Mode | **Geometric** | Default; works well for mobile |
| Initial Memory Size | **32 MB** | Low initial allocation — important for iOS Safari. Profile and raise if needed. |
| Maximum Memory Size | **512 MB** | Sufficient for a UI-only app; keeps iOS Safari happy |
| Geometric Memory Growth Step | **0.2** (default) | |
| Geometric Memory Growth Cap | **96 MB** (default) | |

> **iOS Safari memory constraint:** iOS gives the browser a hard memory limit (~1.5–2 GB total, shared across tabs). Unity's heap growth causes allocation failures if the OS can't find a contiguous block. For a UI-only app, 32–64 MB initial is correct; don't pre-allocate 256 MB.

**Other Settings — Optimization:**

| Setting | Value | Rationale |
|---------|-------|-----------|
| Strip Engine Code | **Enabled** | IL2CPP only — removes unused engine modules, reduces build size |
| Managed Stripping Level | **Medium** | Good balance; test thoroughly after enabling |
| Prebake Collision Meshes | **Disabled** | No physics in this project |

**Other Settings — Rendering:**

| Setting | Value | Rationale |
|---------|-------|-----------|
| Texture compression format | **ASTC** (mobile build) or **DXT** (desktop) | ASTC is hardware-accelerated on iOS/Android. For a single build, use ASTC for mobile. |

> **Separate desktop / mobile builds** are the cleanest option for texture compression. For a wedding app where size matters, build ASTC for mobile. If you need one URL for both, use the default DXT and accept slightly larger mobile textures.

### Loading Time Optimizations

1. **Keep `.data` file small**: Strip unused assets. This app has no 3D models or large audio — the build should be compact (<20 MB uncompressed target).
2. **Enable Data Caching**: Returning guests don't re-download.
3. **Set Application.targetFrameRate = 30** after initial load: For a wedding info app, 30 fps is indistinguishable from 60 fps and halves GPU/battery usage on mobile.

```csharp
// In App.cs Awake() or after first scene loads:
Application.targetFrameRate = 30;
```

### Mobile Browser Gotchas

| Issue | Platform | Details |
|-------|----------|---------|
| Audio autoplay blocked | iOS Safari | Audio requires user gesture to start. Don't play music in `Start()` — use first tap/button press. |
| Keyboard covering canvas | Mobile | `TouchScreenKeyboard` works in WebGL but may behave oddly. For a RSVP form, consider native input fields via jslib. |
| WebGL canvas not filling safe area | iPhone notch | Safe area CSS must be set in the Unity WebGL template. Use `env(safe-area-inset-*)` in CSS. |
| Scroll on mobile | iOS Safari | The WebGL canvas consumes all touch events. Scrollable UGUI content works with ScrollRect but page-level scroll is blocked. |
| Tab throttling | All browsers | Background tabs are throttled to ~1 fps. Expected behavior; don't write code that requires real-time updates while backgrounded. |

---

## Question 5: UnityWebRequest from WebGL — CORS

### Confidence: HIGH (directly from Unity 6.0 networking docs, built 2026-04-16)

### How It Works

`UnityWebRequest` in WebGL uses the **JavaScript Fetch API** under the hood (not a native socket). This means:
- All requests go through the browser's CORS engine
- The browser enforces same-origin policy for cross-domain requests
- The remote server MUST return `Access-Control-Allow-Origin: *` (or specific origin) on its response

### Simple vs. Preflighted CORS Requests

The browser sends a CORS **preflight** (OPTIONS) request before the actual request ONLY for "non-simple" requests. Simple requests bypass preflight.

**Simple request criteria (no preflight):**
- Method: GET, HEAD, POST
- Content-Type is one of: `text/plain`, `application/x-www-form-urlencoded`, `multipart/form-data`
- No custom headers beyond `Accept`, `Content-Type`, `Content-Language`, `Accept-Language`, `DPR`

**Non-simple (triggers preflight):**
- Content-Type: `application/json` ← the most common mistake
- Any custom headers (e.g., `X-Auth-Token`, `Authorization`)

> **Apps Script does NOT respond to OPTIONS preflight requests.** If you send a request with `Content-Type: application/json` or any custom header, the preflight fails and the request is blocked. This is why GET is the right approach for Apps Script.

### Request Pattern Matrix

| Use Case | Method | Content-Type | Preflight? | Works with Apps Script? |
|----------|--------|-------------|------------|------------------------|
| RSVP submit (GET + params) | GET | — | No | ✅ Yes |
| RSVP submit (POST + JSON) | POST | application/json | **Yes** | ❌ No |
| RSVP submit (POST + text/plain) | POST | text/plain | No | ⚠️ Redirect loses body |
| RSVP submit (POST + form-encoded) | POST | application/x-www-form-urlencoded | No | ⚠️ Redirect may lose body |

### Required Server Headers for Cross-Domain Requests

If calling any non-Apps-Script server in future, the server must return:

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Accept, Content-Type
```

Apps Script with "Anyone, even anonymous" deployment returns these automatically on the final redirected response.

### Error Diagnosis

If you see this in browser console:
```
Cross-Origin Request Blocked: The Same Origin Policy disallows reading the remote resource at ...
```

Checklist:
1. Is the server returning `Access-Control-Allow-Origin`? Check Network tab for response headers.
2. Is a preflight (OPTIONS) being sent and failing? Look for a failing OPTIONS request before the actual request.
3. Is the request hitting the right URL? `Application.absoluteURL` + APPS_SCRIPT_URL logged at startup.

### `UnityWebRequest` Code Patterns

```csharp
// ✅ GET request — no CORS issues, simple request
using var req = UnityWebRequest.Get(url);
yield return req.SendWebRequest();
if (req.result != UnityWebRequest.Result.Success)
    Debug.LogError("Request failed: " + req.error);

// ✅ POST with WWWForm — simple CORS (multipart/form-data), but Apps Script
//    redirect will convert to GET, losing form body. Don't use for Apps Script.
var form = new WWWForm();
form.AddField("key", "value");
using var req = UnityWebRequest.Post(url, form);
yield return req.SendWebRequest();

// ❌ POST with JSON — triggers CORS preflight, WILL fail with Apps Script
using var req = new UnityWebRequest(url, "POST");
req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonString));
req.SetRequestHeader("Content-Type", "application/json"); // ← causes preflight
```

---

## Summary — Implementation Decisions

| Decision | Choice | Rationale | Confidence |
|----------|--------|-----------|------------|
| URL param parsing | Pure C# on `Application.absoluteURL` | No jslib needed for GitHub Pages hosting | HIGH |
| Hosting | GitHub Pages | Direct URL control, `Application.absoluteURL` works, free | HIGH |
| RSVP transport | GET request with URL query params | Avoids POST→redirect→body-loss issue with Apps Script | HIGH |
| Apps Script deploy | "Execute as Me", "Anyone, even anonymous" | Required for CORS; no auth challenge for guests | HIGH |
| Password persistence | `PlayerPrefs` (IndexedDB) + explicit `Save()` | Correct for WebGL; warn team about Safari private mode | HIGH |
| Mobile memory | 32 MB initial, Geometric growth, max 512 MB | Avoids iOS Safari contiguous allocation failures | HIGH |
| Compression | Gzip (GitHub Pages default) | Brotli requires server config complexity; gzip works out of the box | MEDIUM |
| Texture compression | ASTC for mobile-targeted build | Hardware-accelerated on all target devices | HIGH |
| Frame rate | 30 fps after init | Saves mobile battery; UI app doesn't need 60 fps | HIGH |
| CORS content-type | Never set `application/json` for Apps Script calls | Avoids preflight; Apps Script doesn't handle OPTIONS | HIGH |

---

## Known Gotchas Summary

1. **`Application.absoluteURL` is empty in Unity Editor** — always gate with `#if UNITY_WEBGL && !UNITY_EDITOR`
2. **Apps Script POST loses body on 302 redirect** — use GET with params instead
3. **`PlayerPrefs.Save()` must be called explicitly** after every write in WebGL
4. **iOS Safari Private mode clears IndexedDB on tab close** — guests will re-enter password
5. **Apps Script preflight failure** — never use `Content-Type: application/json` or custom headers when calling Apps Script
6. **Apps Script redeployment required** — code changes require a new deployment version, the `/exec` URL stays the same
7. **itch.io iframe breaks `Application.absoluteURL` query params** — use GitHub Pages instead
8. **WebAssembly 2023 requires Safari 16.4+** — Safari 16.4 is from March 2023; anyone on pre-iOS 16.4 won't get SIMD perf gains but the build still runs (graceful fallback if WebAssembly 2023 is enabled)
9. **Audio autoplay blocked on iOS** — first audio must be triggered by user gesture
10. **Apps Script reserved params `c` and `sid`** — do not use these as RSVP field names; they return HTTP 405

---

## Sources

| Topic | Source | Date |
|-------|--------|------|
| Unity WebGL memory | https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-memory.html | 2026-04-16 |
| Unity WebGL browser scripting | https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-interactingwithbrowserscripting.html | 2026-04-16 |
| Unity WebGL player settings | https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsWebGL.html | 2026-04-16 |
| Unity WebGL networking / CORS | https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-networking.html | 2026-04-16 |
| Unity WebGL performance | https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-performance.html | 2026-04-16 |
| Unity jslib calling | https://docs.unity3d.com/6000.0/Documentation/Manual/web-interacting-browser-js-to-unity.html | 2026-04-16 |
| Google Apps Script web apps | https://developers.google.com/apps-script/guides/web | 2026-04-01 |
| Google Apps Script quotas | https://developers.google.com/apps-script/guides/services/quotas | 2026-04-01 |
| Google ContentService | https://developers.google.com/apps-script/reference/content/content-service | 2026-04-13 |
