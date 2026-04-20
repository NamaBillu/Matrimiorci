# Domain Pitfalls: Unity 6 WebGL Wedding App

**Domain:** Unity 6 WebGL — browser + mobile browser, UI app, Google Sheets integration
**Researched:** April 17, 2026
**Sources:** Unity 6.0 Manual (built 2026-04-16), Google Apps Script official docs (updated 2026-04-01), verified against known browser behavior

---

## CRITICAL PITFALLS

Mistakes that cause the app to break silently at the wedding — guests see nothing, music never plays, RSVP submits to nowhere.

---

### PITFALL C-1: Audio Never Plays on iOS Safari

**What goes wrong:** Background music and SFX are completely silent when the app loads on iPhone. `AudioSource.Play()` is called correctly in code, but browsers block all audio until the user triggers a physical interaction (tap, click, key press). On iOS Safari this policy is strict with zero workarounds — there is no browser flag to disable it.

**Why it happens:** All major browsers (Chrome, Safari, Firefox) enforce the Web Audio API autoplay restriction. Unity's manual explicitly warns: *"browsers don't allow audio playback until an end user interacts with your application webpage via a mouse click, touch event, or key press."* The Unity audio context remains suspended until a user gesture resumes it. Loading-screen animations that trigger programmatically do **not** count.

**Second layer — iOS Silent Mode:** If the user has the physical mute switch active on iPhone, audio clips with `loadType = DecompressOnLoad` are inaudible even after a user gesture. Unity's docs confirm: *"The sounds aren't audible on iOS devices in Silent Mode because WebKit categorizes this node type differently than `MediaElementSourceNode`."* The `CompressedInMemory` load type uses `MediaElementSourceNode` which respects the Silent Mode setting gracefully (silenced, not broken), while `DecompressOnLoad` silently fails to play.

**Consequences:**
- Music never starts — guests experience a silent app
- SFX on button taps never fire on first load
- On iOS with Silent Mode: music silently fails even after interaction

**Warning signs:**
- "Music doesn't play on iPhone" reports during testing
- `AudioSource.isPlaying` returns `false` immediately after `Play()` call at startup
- No audio errors in browser console — it just does nothing

**Prevention:**
1. **Gate all audio start behind a loading screen tap.** The "tap to continue" button must be the first user interaction. Call `AudioListener.Resume()` or trigger audio from within that button's `OnPointerDown` (not `OnPointerUp`). Unity's deferred event system means the request fires on the next user event — using `OnPointerDown` ensures the follow-up `OnPointerUp` event is available.
2. **Set all AudioClips to `CompressedInMemory` load type.** For background music (which is the only audio in this app) this is the correct setting per Unity docs — lower precision but works in Silent Mode, uses less memory than DecompressOnLoad.
3. **Test on a real iPhone with Silent Mode enabled before launch.**

**Phase that must address this:** Any phase that initializes SoundManager or plays audio. The loading screen / Bootloader phase must implement the user-gesture gate before audio is ever attempted.

---

### PITFALL C-2: Google Apps Script RSVP Submission Returns HTML Instead of JSON (The Redirect Trap)

**What goes wrong:** `UnityWebRequest.Post()` to the Apps Script `/exec` URL appears to succeed but `downloadHandler.text` contains an HTML page or a "302 Found" redirect, not the expected JSON response. The RSVP appears to submit but nothing arrives in Google Sheets.

**Why it happens — the redirect chain:** Google Apps Script's `/exec` endpoint returns an HTTP `302 Found` redirect when first hit. The browser's Fetch API (which Unity WebGL uses internally for `UnityWebRequest`) follows this redirect. However, per the HTTP spec, a 302 redirect changes a POST into a GET on the redirected URL. This means `doPost()` in your Apps Script is **never called** — `doGet()` is called instead. If you only implemented `doPost`, the script has no matching handler and returns an error HTML page.

**Why it happens — the CORS preflight trap:** Setting `Content-Type: application/json` on a cross-origin request triggers a CORS preflight (OPTIONS request). Google Apps Script web apps do **not** handle OPTIONS requests — they return a non-CORS response to the preflight, causing the browser to block the actual POST before it even sends. Unity's console shows `"Cross-Origin Request Blocked"` with no indication the content-type was the trigger.

**Why it happens — `text/plain` response required:** Even if POST works, Apps Script returns the response with the content-type you declare in `setMimeType()`. If you use `MimeType.JSON` but your request triggered a preflight (due to `application/json` content-type), the response is blocked. The safe pattern is `MimeType.TEXT` for the response even if the content is JSON-formatted — the Unity side parses it as a string regardless.

**Consequences:**
- RSVP form submissions silently lost
- No guest RSVPs in Google Sheets on wedding day
- Debugging is hard because Unity's web console shows a CORS error without explaining which header triggered it

**Warning signs:**
- `request.result == UnityWebRequest.Result.ConnectionError` with message "CORS"
- `downloadHandler.text` starts with `<!DOCTYPE html>` instead of `{`
- Google Sheets receives 0 rows after testing

**Prevention — complete working pattern:**

**Apps Script side (`Code.gs`):**
```javascript
function doPost(e) {
  try {
    // Parse URL-encoded form body (NOT JSON)
    var data = e.parameter;
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName("RSVPs");
    sheet.appendRow([
      new Date(),
      data.group || "",
      data.names || "",
      data.attending || "",
      data.meal || "",
      data.notes || ""
    ]);
    return ContentService
      .createTextOutput(JSON.stringify({ status: "ok" }))
      .setMimeType(ContentService.MimeType.TEXT); // TEXT not JSON — avoids CORS issues
  } catch(err) {
    return ContentService
      .createTextOutput(JSON.stringify({ status: "error", message: err.toString() }))
      .setMimeType(ContentService.MimeType.TEXT);
  }
}

// Also implement doGet as a fallback (302 redirect may call this instead of doPost)
function doGet(e) {
  return doPost(e);
}
```

**Unity C# side:**
```csharp
// WRONG — triggers CORS preflight, blocked by Apps Script:
// request.SetRequestHeader("Content-Type", "application/json");

// CORRECT — URL-encoded form data, no preflight triggered:
var form = new WWWForm();
form.AddField("group", groupName);
form.AddField("names", guestNames);
form.AddField("attending", attending.ToString());
form.AddField("meal", mealPreference);
form.AddField("notes", notes);
using var request = UnityWebRequest.Post(appsScriptUrl, form);
// WWWForm automatically sets Content-Type: application/x-www-form-urlencoded
// This is a "simple request" — no CORS preflight required
yield return request.SendWebRequest();
```

**Deployment checklist:**
- Deploy as "Execute as: Me", "Who has access: Anyone" (no login required)
- After any code change, create a **new deployment** (not update existing) — old URLs are cached
- Test the deployment URL with a browser `?group=test&names=test` GET request first

**Phase that must address this:** RSVP submission phase. Must be integration-tested against the live Apps Script URL before the wedding.

---

### PITFALL C-3: PlayerPrefs Lost When Guest Opens App in iOS Private Browsing

**What goes wrong:** A guest enters their password correctly, gets personalized content, then closes the tab and reopens the QR code link. Their password is gone and they must enter it again. Worse: in iOS Safari private browsing, `PlayerPrefs.Save()` (which calls `localStorage.setItem()`) may throw a `QuotaExceededError` silently — the password appears saved in-memory during the session but is never persisted.

**Why it happens:** In Unity WebGL, `PlayerPrefs` maps directly to the browser's `localStorage`. On iOS Safari in private browsing mode, `localStorage` is sandboxed per private session:
- Storage is available but the quota is severely reduced
- All data is cleared when the last private tab in that window is closed
- In some iOS versions, writes fail silently when storage is near-full

On any browser in incognito/private mode (Chrome, Firefox, Edge), `localStorage` is cleared when all incognito windows close. This is expected browser behavior — guests who use private mode by habit will lose their password on every visit.

**A separate issue — `PlayerPrefs.Save()` must be called explicitly in WebGL.** On desktop platforms, `PlayerPrefs` auto-saves. On WebGL, `OnApplicationQuit()` is never reliably called when a browser tab closes. If `PlayerPrefs.Save()` is not called immediately after `PlayerPrefs.SetString()`, the data may not be flushed to `localStorage` before the tab closes.

**Consequences:**
- Guests have to re-enter passwords every visit if using private mode
- If using `PlayerPrefs` to remember form state (partially filled RSVP), that state is also lost
- Frustrating UX for the ~20% of mobile users who default to private browsing

**Warning signs:**
- "I entered the password but had to enter it again" reports during testing on iPhone
- Testing in Chrome incognito shows password gone after closing tab
- `PlayerPrefs.GetString()` returns empty on second load after a previous successful session

**Prevention:**
1. **Always call `PlayerPrefs.Save()` immediately after every `PlayerPrefs.Set*()` call.** Never rely on auto-save in WebGL. Pattern:
   ```csharp
   PlayerPrefs.SetString("password_group", groupName);
   PlayerPrefs.Save(); // ← mandatory on WebGL
   ```
2. **Detect private mode failure gracefully.** Wrap PlayerPrefs writes in a try/catch via a JS interop helper, or simply re-prompt for the password if `GetString()` returns empty. For a wedding app with <50 guests, requiring re-entry is acceptable.
3. **Do not gate critical content exclusively on PlayerPrefs.** The password popup must always be accessible. Guests in private mode will re-enter each visit — design for this.
4. **Never store anything that would be catastrophic to lose.** For this app: passwords are fine to lose (guests re-enter). Do not attempt to persist partially-filled RSVP form state in PlayerPrefs — the risk of data loss exceeds the convenience.

**Phase that must address this:** Password storage phase. Call `PlayerPrefs.Save()` after every write, and test the re-entry flow explicitly.

---

## MODERATE PITFALLS

Mistakes that degrade the experience but don't fully break the app.

---

### PITFALL M-1: Build Is Too Large to Load on Mobile (Guests Give Up)

**What goes wrong:** The default Unity 6 WebGL build ships with a `.wasm` file of 20-40MB, a `.data` file containing all scene assets, and uncompressed or poorly-compressed files. On a mobile connection at a wedding venue (often congested Wi-Fi or 4G), a 50MB+ uncompressed build can take 30+ seconds to load. Guests abandon the page before the loading bar completes.

**Why it happens — the five biggest contributors:**
1. **Development build mode left on.** Development builds are uncompressed and unminified — 2-3x larger than release builds. Easy to ship accidentally.
2. **No Brotli compression.** Unity supports Brotli, Gzip, or Disabled compression. Brotli achieves ~20% better compression than Gzip. A 25MB wasm file becomes ~7MB with Brotli.
3. **Wrong Code Optimization setting.** The default is not `Disk Size with LTO` — it must be set explicitly. LTO (Link Time Optimization) removes dead code across the entire binary.
4. **Strip Engine Code disabled.** Strips unused Unity subsystems (physics, animation rigs, etc.). For a 2D UI app, this removes significant code.
5. **Uncompressed or high-quality textures.** UI sprite atlases at 2048×2048 uncompressed RGBA32 add megabytes unnecessarily.

**Warning signs:**
- Build folder `.wasm.br` file is larger than 10MB for a simple UI app
- Loading takes >10s on a phone with good Wi-Fi
- The `.data` file is larger than 5MB for a 2-scene app

**Prevention — mandatory build settings checklist:**
```
File > Build Profiles > Web:
  ☑ Development Build: OFF
  ☑ Code Optimization: Disk Size with LTO

Edit > Project Settings > Player > Web > Publishing Settings:
  ☑ Compression Format: Brotli
  ☑ Strip Engine Code: ON
  ☑ Enable Exceptions: None (disable if no exception handling needed)

Edit > Project Settings > Player > Web > Other Settings:
  ☑ Managed Stripping Level: High
```

**Server configuration for Brotli (critical — often forgotten):** Brotli-compressed files are pre-compressed by Unity. The server must serve them with `Content-Encoding: br` or browsers will try to parse the compressed binary as plain text and fail. Unity provides server configuration samples for nginx, Apache, and IIS in the manual. If using GitHub Pages or a static host that doesn't support configuring headers, use **Gzip** instead (all static hosts support it natively).

**Target sizes for this app:**
- `.wasm.br` (or `.wasm.gz`): < 8MB
- `.data.br` (or `.data.gz`): < 3MB for a 2-scene UI app
- Total download: < 12MB

**Phase that must address this:** Build pipeline / deployment phase. Also: audit textures in the asset preparation phase (use ASTC 8×8 for UI sprites on mobile).

---

### PITFALL M-2: UGUI InputField on Mobile WebGL Causes Keyboard / Layout Issues

**What goes wrong:** When a guest taps a UGUI `InputField` (e.g., the dietary restrictions free-text field in the RSVP form) on a mobile browser, the native virtual keyboard appears. This causes the browser viewport to shrink, which may push the Unity canvas up, partially off-screen, or cause elements to overlap. The Unity canvas does not automatically resize to fit the new viewport — the UI becomes unusable.

**Second issue — `HideMobileInput` is ignored.** In the Unity Editor, `TMP_InputField.HideMobileInput = true` hides the native input overlay on some platforms. On WebGL, **this option has no effect** — Unity's manual states explicitly: *"The HideMobileInput option in TextMeshPro (also known as TMP) input fields has no effect on the Web platform."* The native text input field always appears to trigger the virtual keyboard.

**Third issue — keyboard dismiss doesn't always resize viewport back.** After the keyboard dismisses on iOS Safari, the viewport may remain smaller than the original size, leaving a gap at the bottom of the page.

**Warning signs:**
- Testing on Chrome DevTools mobile emulation does not reproduce this — must test on a real device
- Bottom UI elements disappear or become untappable after keyboard opens
- The canvas shrinks when keyboard appears

**Prevention:**
1. **Minimize the number of free-text InputFields.** This app only needs one (notes/dietary restrictions). Replace optional fields with toggle buttons wherever possible.
2. **Disable mobile keyboard globally if free text is not needed.** If the notes field can be removed or deferred, call `WebGLInput.mobileKeyboardSupport = false` in Bootloader to prevent the keyboard from ever opening.
3. **If keyboard is needed:** Add a JavaScript resize handler via `[DllImport("__Internal")]` to notify Unity when the viewport changes, then have the CanvasScaler respond. Alternatively, use a fixed-bottom layout that the keyboard does not overlap.
4. **Test RSVP form on a real iPhone before launch.** Specifically: open the notes field, type something, dismiss keyboard, verify layout is intact.

**Phase that must address this:** RSVP form implementation phase.

---

### PITFALL M-3: UnityWebRequest Fails Silently on Low-End / Slow Connections

**What goes wrong:** The RSVP form is submitted, `UnityWebRequest.SendWebRequest()` starts, but the request times out on a slow or congested wedding venue network. The guest sees no feedback. The default `UnityWebRequest` timeout is 0 (no timeout) — the coroutine waits forever, blocking the UI.

**Second issue — no retry on network failure.** If the Apps Script takes 5+ seconds to respond (cold start latency is common — Apps Script functions often take 3-8 seconds on first run after idle), the user may navigate away thinking the submit failed.

**Warning signs:**
- RSVP submission hangs indefinitely during testing on slow Wi-Fi
- No error message appears when network is unavailable
- Cold-start Apps Script response takes 5+ seconds on first run of the day

**Prevention:**
1. **Always set a timeout:**
   ```csharp
   request.timeout = 15; // 15 seconds max
   ```
2. **Show clear in-progress feedback.** Show a spinner or "Invio in corso..." text immediately on submit tap. Replace with "Inviato!" on success or "Errore di rete — riprova" on failure.
3. **Implement a simple retry.** On failure, show a retry button rather than forcing the user to re-fill the form. Cache the form data in a local variable, not in the form fields.
4. **Pre-warm Apps Script.** Make a no-op GET request to the Apps Script URL when the app loads (after first user interaction), so the function is warm by the time the user submits. This reduces the cold-start delay.

**Phase that must address this:** RSVP submission phase.

---

### PITFALL M-4: iOS Safari Crashes or Refuses to Load Due to Memory

**What goes wrong:** On older iPhones (iPhone 8, SE 2nd gen, older iPad) or when other browser tabs are open, iOS Safari kills the Unity WebGL tab with a "A problem repeatedly occurred on [URL]" error, or the page simply goes blank. This is a browser-enforced out-of-memory kill.

**Why it happens:** iOS Safari has strict per-tab memory limits that vary by device and available system RAM. Unity's `.data` file is decompressed into a virtual in-memory filesystem (allocated by JavaScript/Emscripten) in addition to the Unity heap itself. For a typical Unity 6 WebGL build, the total browser-side allocation at runtime can be 150-400MB. On an iPhone SE 2 with 3GB RAM and multiple apps open, this is enough to trigger a tab kill.

**Additionally:** Unity heap auto-resize can fail on mobile if the browser cannot allocate a contiguous memory block. Unity's docs state: *"Automatic resizing of the heap can cause your application to crash if the browser fails to allocate a contiguous memory block."* The recommendation: *"For mobile browsers, it's recommended to configure the Initial Memory Size to the typical heap usage of the application."*

**Warning signs:**
- Page goes blank or shows a white screen on iPhone after partial loading
- Works fine on desktop Chrome but fails on older iPhone
- Error in Safari browser console: "Out of memory" or WebAssembly instantiation error

**Prevention:**
1. **Reduce build size aggressively** (see Pitfall M-1). Every MB saved directly reduces peak memory at runtime. Target < 12MB total download.
2. **Set Initial Memory Size explicitly** in Player Settings > Web > Memory Settings. Profile the app on a target mobile device using Safari's Web Inspector, then set `Initial Memory Size` to that measured value + 20% headroom. Do not leave it at the default (which is tuned for desktop).
3. **Disable unused engine subsystems.** A UGUI app does not need the Physics 3D module, Terrain, etc. Use `Strip Engine Code: ON` and `Managed Stripping Level: High` to remove them.
4. **Do not load all scenes into memory simultaneously.** Use `LoadSceneMode.Single` (default), not Additive, unless there's a specific reason.
5. **Test on the oldest/weakest device in the guest list** before the wedding. If an iPhone SE is expected, test on one.

**Phase that must address this:** Build optimization phase. Also: do a pre-launch memory test on the weakest target device.

---

### PITFALL M-5: QR Code URL Query Params Break or Are Stripped

**What goes wrong:** The printed QR code points to `https://yourdomain.com/?type=invite`. When guests scan it, they get the home page instead of the invite scene — or worse, a 404. This happens because:
- The hosting platform strips query parameters from the URL before serving the file
- The QR code was generated from an HTTP URL (some generators convert to HTTPS but strip params)
- The URL contains a hash `#type=invite` instead of `?type=invite`, which `Application.absoluteURL` may not parse the same way

**Second issue — Italian names in URL params.** If you ever add guest-name pre-fill via URL params (e.g., `?names=Giàn+%26+Maria`), special characters like `à`, `è`, `&`, `+` must be correctly URL-encoded. Un-encoded ampersands split the parameter at the wrong boundary.

**Third issue — URL length vs QR code density.** Very long URLs create denser QR codes that are harder to scan in low-light conditions (wedding venue candle light). Every extra character in the URL adds complexity.

**Warning signs:**
- Testing by manually typing the URL works but scanning the QR code fails
- The QR code points to HTTP and some guests get a "Not Secure" warning or redirect
- `Application.absoluteURL` parsed in Bootloader doesn't contain the `type` param

**Prevention:**
1. **Use HTTPS always.** HTTP is required for any sensor access (accelerometer, gyroscope), but more importantly: some browsers downgrade HTTP QR scans, and iOS Safari may block non-HTTPS content. Deploy to HTTPS from day one.
2. **Keep query params minimal.** `?type=home` and `?type=invite` are short and safe. Do not put names or guest data in QR code URLs — use the password system instead.
3. **Test the QR code before printing.** Scan it with multiple devices (iPhone Safari, Android Chrome) the week before printing, not the night before the wedding.
4. **Verify `Application.absoluteURL` parsing with the exact printed URL.** Log the full URL in Bootloader during development and confirm the `?type=` param is present.
5. **Avoid URL shorteners.** Short URLs add a redirect hop. If the shortener strips query params or goes down, the QR codes on all physical invites break permanently. Use the full URL directly, or use a domain you control with your own redirect rules.
6. **Generate QR codes at the highest error correction level (H).** This allows a QR code to be readable even if 30% is obscured (sticker, damage). Worth the slight density increase.

**Phase that must address this:** Bootloader URL routing phase and physical invite preparation.

---

## MINOR PITFALLS

Common annoyances that are easy to miss but quick to fix.

---

### PITFALL m-1: UGUI ScrollRect Has No Touch Momentum on Mobile WebGL

**What goes wrong:** A `ScrollRect` (e.g., a scrollable list of wedding schedule items or programme) scrolls correctly in the Editor with mouse wheel but feels "dead" on mobile — no momentum, snaps to stop immediately when the finger lifts. This is because UGUI's `ScrollRect` implements its own momentum/inertia in C# that may not integrate correctly with the browser's touch event timing.

**Prevention:** Set `ScrollRect.movementType = MovementType.Elastic` and `ScrollRect.decelerationRate = 0.135f` (the default). If it still feels wrong on device: test with `movementType = MovementType.Clamped` for fixed-content lists, which removes the momentum illusion entirely. For a wedding programme list, clamped scrolling is preferable to broken momentum scrolling.

**Phase that must address this:** Home scene UI implementation.

---

### PITFALL m-2: `PlayerPrefs.Save()` Called from `OnApplicationQuit()` Is Unreliable in WebGL

**What goes wrong:** You call `PlayerPrefs.Save()` in `OnApplicationQuit()` to persist data when the browser tab closes. This never fires reliably in WebGL. Browsers do not guarantee any callback before a tab is closed — the tab is killed without warning.

**Prevention:** Call `PlayerPrefs.Save()` immediately and synchronously after every `PlayerPrefs.Set*()` call. Never defer to `OnApplicationQuit()` for WebGL persistence. See Pitfall C-3.

**Phase that must address this:** Any phase that writes PlayerPrefs.

---

### PITFALL m-3: Safe Area Padding Breaks on Some Android Devices

**What goes wrong:** The safe area inset for the bottom home indicator (iPhone) or Android navigation bar is applied, but on some Samsung or Xiaomi phones with software navigation bars, `Screen.safeArea` returns incorrect values. UI elements are either clipped by the navigation bar or padded too aggressively, leaving a large empty gap.

**Prevention:** Test with Android's gesture navigation (full-screen mode) separately from button navigation mode. Do not rely on `Screen.safeArea.yMin` being zero on Android — some OEM overlays report non-zero values even without a notch. Cap the safe area inset to a reasonable maximum (e.g., 120px) to prevent over-padding on unusual devices.

**Phase that must address this:** Safe area / layout phase (already marked as validated in PROJECT.md, but worth re-testing on Android after RSVP form is added).

---

### PITFALL m-4: `Application.absoluteURL` Returns Empty String in Editor

**What goes wrong:** URL routing code that calls `Application.absoluteURL` and parses `?type=home` crashes with a null reference or routes to the default scene when running in the Unity Editor, because `Application.absoluteURL` returns an empty string in the Editor (it's only populated in a real WebGL build).

**Prevention:** Guard all URL parsing with a platform check:
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    string url = Application.absoluteURL;
    // parse ?type= param
#else
    // In Editor: default to home scene or use a test override variable
    string url = "?type=home"; // configurable Editor default
#endif
```

**Phase that must address this:** Bootloader URL routing implementation (already in active requirements).

---

### PITFALL m-5: WebGL Build Cached by Browser — Guests Load Old Version After Hotfix

**What goes wrong:** You push a hotfix to the wedding app (e.g., fix a wrong venue address) the morning of the wedding. Guests who already loaded the page earlier that day get the old cached version because the browser cached the `.wasm` and `.data` files. Unity's default build output uses fixed filenames (`Build.wasm`, `Build.data`) — the browser sees the same filename and returns the cached copy.

**Why this matters at a wedding:** You cannot ask 50 guests to clear their browser cache during cocktail hour.

**Prevention:**
1. **Enable `Cache Control: no-store` or use versioned filenames** on the deployment server for Unity build files. Most static hosts (Netlify, GitHub Pages) support cache busting via filename hashing.
2. **Unity 6 Unity Web builds already append a hash to filenames** by default in release builds (`Build.abc123.wasm.br`). Verify that your hosting configuration does not strip this hash.
3. **Test a redeployment end-to-end** during pre-launch week: deploy, load in browser, redeploy a change, hard-refresh the page, confirm the new version loads.

**Phase that must address this:** Deployment and hosting phase.

---

## Phase-Specific Warning Matrix

| Phase Topic | Highest-Risk Pitfall | Must-Do Before Launch |
|-------------|---------------------|----------------------|
| Bootloader / URL routing | m-4: `absoluteURL` empty in Editor | Test on built WebGL, not in Editor |
| Sound Manager initialization | C-1: Audio blocked without user gesture | Gate all audio on first user tap; use `CompressedInMemory` |
| Password + PlayerPrefs | C-3: Private mode wipes localStorage | Call `PlayerPrefs.Save()` after every write; test in Incognito |
| RSVP form UI | M-2: Mobile keyboard breaks layout | Test on real iPhone with notes field |
| RSVP submission | C-2: Apps Script CORS / redirect trap | Use `WWWForm` (not JSON body); implement `doGet` fallback |
| RSVP submission | M-3: Silent timeout on slow network | Set `request.timeout = 15`; show spinner and retry button |
| Build optimization | M-1: Build too large for mobile | Brotli + LTO + Strip Engine Code; target < 12MB total |
| Build optimization | M-4: iOS memory crash | Profile on weakest target device; tune Initial Memory Size |
| QR code / invites | M-5: Query params stripped or broken | Scan test on iPhone + Android before printing |
| Deployment | m-5: Cached old version after hotfix | Verify filename hashing or cache-control headers |

---

## Sources

- Unity 6.0 Manual — Audio in Web: https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-audio.html (built 2026-04-16)
- Unity 6.0 Manual — Memory in Unity Web: https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-memory.html (built 2026-04-16)
- Unity 6.0 Manual — Web Networking: https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-networking.html (built 2026-04-16)
- Unity 6.0 Manual — Input in Web: https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-input.html (built 2026-04-16)
- Unity 6.0 Manual — Distribution size and code stripping: https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-distributionsize-codestripping.html (built 2026-04-16)
- Unity 6.0 Manual — Optimize Web platform for mobile: https://docs.unity3d.com/6000.0/Documentation/Manual/web-optimization-mobile.html (built 2026-04-16)
- Google Apps Script — Web Apps guide: https://developers.google.com/apps-script/guides/web (updated 2026-04-01)
- MDN — localStorage behavior in private browsing: well-documented browser behavior, consistent across all major browsers
- Web Audio API autoplay policy: https://developer.chrome.com/blog/autoplay/ (referenced by Unity 6 audio docs)
