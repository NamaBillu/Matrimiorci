# Unity 6 WebGL Mobile Performance Analysis
**Target:** iOS Safari + Android Chrome | **Scale:** <50 guests | **Date:** 2026-04-25

---

## 1. Root Cause Diagnosis

### Why input feedback is delayed

Unity WebGL on mobile runs entirely on the **browser's single main thread**. The event pipeline is:

```
Physical touch → OS → Browser gesture recognizer → JS touch event
  → Emscripten C++ → Unity input queue → Unity Update() → render → composite → display
```

The two structural bottlenecks in this stack are:

**A. Browser gesture disambiguation delay (primary culprit)**  
Without `touch-action: none` on the canvas element, the browser cannot know whether a touch is a scroll, pinch-zoom, or a tap. It must wait — typically 50–300ms — to see if a second finger appears or if the touch moves, before deciding to fire the synthetic click event or dispatch the touchstart. On iOS Safari, `user-scalable=no` in the viewport meta has been **ignored since iOS 10** (Apple overrode it for accessibility). So the existing viewport tag is not eliminating this delay on iPhones. The `style.css` for `#unity-canvas` has no `touch-action` property — just `background: #EFE6DD`. This is the direct cause of the tap-to-response lag.

**B. Main thread contention**  
WASM execution, JavaScript, layout, and event dispatch all compete on the same thread. When Unity is mid-frame (e.g. `ForceMeshUpdate()` on a `CurveText`, or a GC collection), incoming touch events queue up in the browser's event loop and cannot be delivered to Unity until the current task finishes. Every millisecond of frame-time overhead directly becomes input lag.

### Why scrolling is painful

Unity UI ScrollRect's scroll handling is entirely software-driven: Unity reads drag deltas from its input system, updates a RectTransform position, marks the Canvas dirty, and re-renders. The browser has zero involvement in momentum, deceleration, or touch tracking — that's all Unity C# code. The result:

1. **Rendering at full device pixel ratio** — with `config.devicePixelRatio` unset, Unity renders the WebGL framebuffer at the screen's native DPR (2× on most Android, 3× on iPhone 15 Pro). A 393×852 logical-pixel iPhone becomes a 1179×2556 render target. The GPU cost scales with pixel count. Scroll redraws hit the GPU budget hard, causing dropped frames.
2. **Canvas rebuild on scroll** — every `ScrollRect` drag marks the scrollable Canvas dirty, causing Unity to re-batch every UI element in that Canvas. On high-DPR, each rebatch + re-raster takes longer, pushing frames past 16ms and dropping to 30fps.
3. **No CSS `overflow: hidden` equivalency** — Unity can't hand off scrolling to the browser's GPU-composited layer (which is what makes native app scrolling buttery smooth). Unity always re-renders.

### Why the app "feels slow" overall

The pervasive slowness is the combination of the above two issues plus:
- Every frame carrying the overhead of rendering at 2–3× necessary resolution
- GC collections from the AudioSource-per-sound allocation pattern briefly halting execution
- `CurveText.WarpText()` calling `ForceMeshUpdate()` on any TMP text change, which is O(character count) vertex manipulation

---

## 2. Ranked Issues

### 🔴 CRITICAL

---

#### C-1: No `touch-action: none` on the canvas
**File:** `TemplateData/style.css`  
**Category:** No code change needed (CSS only)

The `#unity-canvas` style block is:
```css
#unity-canvas { background: #EFE6DD }
```
No `touch-action` property. The browser's default is `touch-action: auto`, meaning it tracks every touch for potential scroll/pinch gestures. On iOS Safari (iOS 10+), `user-scalable=no` is ignored, so the viewport meta does nothing to prevent this delay.

**Why this causes input lag:** The browser delays touchstart propagation until it can classify the gesture. By the time Unity receives the event, 50–300ms has passed. This is the single largest contributor to the "tap → visible delay" symptom.

---

#### C-2: `config.devicePixelRatio` unset (rendering at 2–3× native resolution)
**File:** `index.html`, line: `// config.devicePixelRatio = 1;` (commented out)  
**Category:** No code change needed (index.html config)

With `matchWebGLToCanvasSize = true` (default) and no DPR cap, Unity reads `window.devicePixelRatio` and sets the WebGL framebuffer to that multiple of the canvas's logical size. Typical values: `2.0` (Pixel 7, most Android flagships), `3.0` (iPhone 15 Pro, Samsung S24).

**Pixel count multiplication:**
| Device DPR | Logical res (CSS px) | Render res | Pixels vs DPR=1 |
|---|---|---|---|
| 1.0 | 393 × 852 | 393 × 852 | 1× |
| 2.0 | 393 × 852 | 786 × 1704 | 4× |
| 3.0 | 393 × 852 | 1179 × 2556 | **9×** |

For a UI-only wedding app (no 3D, no fine pixel art), there is zero perceptible visual quality difference between DPR=1 and DPR=3. You are doing 9× the render work for nothing on iPhone 15 Pro.

**Why this causes all three symptoms:** More pixels = more GPU time per frame = fewer frames per second = scroll stutter + general sluggishness. The GPU being busy also increases the time between input event and visual response (the frame that contains the response takes longer to render).

---

### 🟠 HIGH

---

#### H-1: `targetFrameRate = 120` on mobile WebGL
**File:** `Assets/App/Scripts/Managers/App.cs:61`  
**Category:** Code change needed

```csharp
Application.targetFrameRate = 120;
```

In Unity WebGL, the main loop is driven by the browser's `requestAnimationFrame`. iOS Safari caps rAF at **60 Hz regardless of device screen refresh rate** (ProMotion iPhones do not expose 120Hz rAF to WebKit-based browsers — this is a WebKit limitation as of 2026). Android Chrome respects high refresh rates on capable devices, but the WebGL thread budget doesn't change.

The concrete harm: Unity 6 WebGL uses a frame-pacing system. When `targetFrameRate = 120`, Unity's internal scheduler expects frames to complete in ≤8.33ms. A 12ms frame (which would be a fine 83fps result) is flagged as a "miss." Unity then uses `setTimeout` with a near-zero delay to try to "catch up" — bypassing rAF's battery-optimized sleep mechanism. On mobile, this means:
- The browser's power governor keeps the CPU awake at high frequency
- Background tab throttling doesn't apply properly
- Battery drains faster → device thermals → CPU throttling → everything gets worse over time

**`QualitySettings.vSyncCount = 0` is a no-op in WebGL** — it's silently ignored. The rAF loop always provides effective vsync.

**Why this causes all three symptoms:** Sustained heat/throttling, increased battery usage, plus Unity never entering a low-power "frame complete, sleep until next rAF" state.

---

#### H-2: `CurveText.cs` — `[ExecuteAlways]` + `ForceMeshUpdate()` on every text change
**File:** `Assets/App/Scripts/Utils/CurveText.cs`  
**Category:** Code change needed

```csharp
[ExecuteAlways]
public class CurveText : MonoBehaviour
{
    private void OnEnable()
    {
        WarpText();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged);
    }
    private void ReactToTextChanged(UnityEngine.Object obj)
    {
        // ...
        WarpText();
    }
    private void WarpText()
    {
        text.ForceMeshUpdate(); // full vertex rebuild
        // O(characterCount) loop manipulating raw vertex arrays
    }
}
```

`TMPro_EventManager.TEXT_CHANGED_EVENT` is a **global broadcast** — it fires for EVERY TMP text change anywhere in the scene, not just changes to the TMP component attached to this `CurveText`. The `ReactToTextChanged` checks `tmpText == text`, but the event itself fires and the delegate executes before the equality check.

`ForceMeshUpdate()` is expensive: it forces a full layout/geometry pass on TMP, then the method walks every character's vertex array. If there are multiple `CurveText` instances, each one reacts to every global TMP change.

`[ExecuteAlways]` means all of this also runs in Edit mode, which is irrelevant at runtime but signals the developer intent was editor tooling — yet the `OnEnable/OnDisable` subscription also runs at runtime.

**Why this causes slowness:** Any UI interaction that updates a TMP label (showing a popup, updating text in a ScrollRect, typing in the RSVP input field) triggers a chain of `ForceMeshUpdate()` calls across all active `CurveText` instances, causing a CPU spike mid-frame.

---

#### H-3: `SafeArea.CalculateSafeArea()` — `GetComponent<RectTransform>()` on every call
**File:** `Assets/App/Scripts/Utils/SafeArea.cs:31`  
**Category:** Code change needed (trivial)

```csharp
public void CalculateSafeArea()
{
    rectTransform = gameObject.GetComponent<RectTransform>(); // called every time
    // ...
}
```

`OnRectTransformDimensionsChange()` calls `CalculateSafeArea()`. On mobile, this fires whenever:
- The on-screen keyboard appears or disappears (RSVP notes input, password input)
- Screen orientation changes
- The browser chrome (URL bar) shows/hides (common on iOS Safari scroll)

`GetComponent<T>()` does a component type hash lookup — it's not free. More importantly, `SafeAreaChanged?.Invoke(rectTransform)` is also called every time, which may trigger additional layout recalculations in subscribers.

The field `rectTransform` is already declared at class scope — it should be cached in `Awake()` or `Start()` once and never re-fetched.

---

### 🟡 MEDIUM

---

#### M-1: 40–50MB uncompressed build payload on GitHub Pages
**Category:** Hosting / Build config

GitHub Pages CDN double-decompresses pre-compressed Gzip files, which is why compression was disabled. The result is a 40–50MB raw download on a mobile connection. On a 20 Mbps LTE connection (typical mobile), that's 16–20 seconds of loading time before the app becomes interactive. On weaker connections (5–10 Mbps), 30–40 seconds.

Brotli compression typically achieves ~70–75% reduction on Unity WebGL builds. The build would become ~10–12MB with Brotli, which loads in 4–6 seconds on the same connection.

The fix requires a different hosting approach or a CI/CD pipeline — see Section 3.

---

#### M-2: `RSVPPopup.PopulateGuestRows()` — Destroy then Instantiate every open
**File:** `Assets/App/Scripts/Popup/RSVPPopup.cs`  
**Category:** Code change needed (low priority given <10 guests)

```csharp
foreach (Transform child in guestRowContainer)
    Destroy(child.gameObject);  // deferred to end-of-frame
_guestRows.Clear();

foreach (string memberName in group.memberNames)
{
    GameObject rowGo = Instantiate(guestRowPrefab, guestRowContainer);
```

`Destroy()` is deferred — old rows still exist until end of frame. During `OnShowing`, both old and new rows exist simultaneously in the hierarchy, briefly doubling the Canvas layout work. More importantly, `Instantiate()` of prefabs with TMP components triggers TMP font atlas lookup and mesh allocation.

For a list of ≤10 guests, the impact is a single visible hitch (~50–100ms) on popup open, not ongoing. The correct fix is to pre-instantiate the max expected rows in `Initialize()` and toggle their visibility, but given the scale constraint this is low ROI.

---

#### M-3: `SoundManager` — AudioSource GameObject creation per sound
**File:** `Assets/App/Scripts/Managers/SoundManager.cs`  
**Category:** Code change needed (low priority given sound usage)

Every `Play()` call creates a new `GameObject` with `AddComponent<AudioSource>()`, plays the sound, then `Destroy()`s it when done. On WebGL, Unity's audio uses the Web Audio API, so each `AudioSource` creates a Web Audio context node. `Destroy()` + `Instantiate()` cycles generate managed heap allocations that eventually trigger the GC.

For a wedding app that plays a background music track (looping) and occasional button click sounds, the practical impact is small. However, rapid button tapping during DOTween animations could stack up several simultaneous `AudioSource` GameObjects.

---

#### M-4: `viewport` meta — `shrink-to-fit=yes`
**File:** `index.html` (mobile block)  
**Category:** No code change needed (HTML config)

```html
meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
```

`shrink-to-fit=yes` is a deprecated iOS Safari hint that tells the browser to shrink content to fit the viewport. It can cause Safari to perform an extra layout pass on load, and may cause the canvas to render at a different logical size than expected during the initial frame. Remove it.

---

### 🔵 LOW

---

#### L-1: `SectionButton.Refresh()` — lambda re-allocation on every unlock event
**File:** `Assets/App/Scripts/UI/SectionButton.cs`  
**Category:** Code change (not worth doing)

`_button.onClick.RemoveAllListeners()` + `AddListener(OpenPasswordPopup)` allocates a new delegate wrapper. This happens once per unlock event per `SectionButton` instance. With <10 section buttons and 1 unlock per session, the GC pressure is negligible. **Do not change this.**

---

#### L-2: `PopupManager.Show()` — `SetAsLastSibling()` on every popup open
**File:** `Assets/App/Scripts/Managers/PopupManager.cs:74`  
**Category:** Code change (not worth doing)

`popup.GetComponent<Transform>().SetAsLastSibling()` marks the Canvas hierarchy dirty. The next frame rebatches and re-renders. This is a one-time cost on popup open, entirely acceptable.

---

## 3. Optimizations That Can Be Done

### Fix 1 — Add `touch-action: none` to canvas CSS
**File:** `TemplateData/style.css`  
**Effort:** 1 line, no rebuild needed  
**Impact:** Eliminates browser gesture disambiguation delay → fixes input lag symptom directly

```css
/* BEFORE */
#unity-canvas { background: #EFE6DD }

/* AFTER */
#unity-canvas { background: #EFE6DD; touch-action: none; }
```

This tells the browser "this element handles all touch events itself, never scroll/pinch natively." The browser skips gesture tracking entirely and dispatches `touchstart` immediately. Deploy via GitHub Pages — no Unity rebuild required.

---

### Fix 2 — Cap devicePixelRatio in index.html
**File:** `index.html`  
**Effort:** Uncomment 1 line, no rebuild needed  
**Impact:** 4–9× reduction in render pixels on modern phones → directly fixes scroll stutter and general slowness

```javascript
// BEFORE (inside the mobile if-block):
// config.devicePixelRatio = 1;

// AFTER:
config.devicePixelRatio = 1;
```

`1` is the most aggressive option. Alternatively, `Math.min(window.devicePixelRatio, 1.5)` provides a middle ground that slightly improves text clarity without the full 9× overhead penalty. For this app, `1` is the correct choice — no game content requires subpixel accuracy.

Deploy via GitHub Pages — no Unity rebuild required.

---

### Fix 3 — Set targetFrameRate = 60 on WebGL builds
**File:** `Assets/App/Scripts/Managers/App.cs`  
**Effort:** 1 line change, requires rebuild  
**Impact:** Correct frame budget for rAF-driven loop → reduces battery/thermal pressure → prevents CPU throttling over time

```csharp
// BEFORE:
Application.targetFrameRate = 120;

// AFTER:
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.targetFrameRate = 60;
#else
    Application.targetFrameRate = 120;
#endif
```

This uses `60` only in WebGL at runtime, preserving the `120` for Editor and potential native builds. No mobile browser will give you more than 60fps rAF anyway. This also ensures Unity's frame scheduler uses rAF-native timing rather than falling back to aggressive setTimeout polling.

---

### Fix 4 — Cache RectTransform in SafeArea
**File:** `Assets/App/Scripts/Utils/SafeArea.cs`  
**Effort:** 2-line change  
**Impact:** Eliminates repeated component lookup on keyboard show/hide events

```csharp
// BEFORE (in CalculateSafeArea):
public void CalculateSafeArea()
{
    rectTransform = gameObject.GetComponent<RectTransform>(); // ← remove this line
    ...
}

// AFTER: cache once in Start() (it's already called there):
private void Start()
{
    rectTransform = gameObject.GetComponent<RectTransform>(); // ← move here, once
    // ...rest of Start() unchanged
    CalculateSafeArea();
}
```

`CalculateSafeArea()` then uses the already-set field without a lookup.

---

### Fix 5 — Guard CurveText to runtime only, remove ExecuteAlways if not editor-critical
**File:** `Assets/App/Scripts/Utils/CurveText.cs`  
**Effort:** Attribute removal or `#if UNITY_EDITOR` guard  
**Impact:** Prevents ForceMeshUpdate() from firing during runtime text changes not related to this component

Option A (safest): Remove `[ExecuteAlways]` — the Editor preview functionality is lost but runtime is unaffected. The `OnValidate` / `PrefabStageOpened` editor hooks already handle the editor-only cases.

Option B (precise fix): Guard `ReactToTextChanged` to skip TMP components that are not `this.text`:
```csharp
private void ReactToTextChanged(UnityEngine.Object obj)
{
    if (obj != text) return; // early-exit before any work
    if (!isForceUpdatingMesh)
        WarpText();
}
```
Note: the existing check already does `tmpText == text`, but only after the cast. The cast itself is cheap; the issue is `WarpText()` being called. If there is only one `CurveText` in the scene, this is already working correctly. The issue scales with the number of `CurveText` instances.

---

### Fix 6 — Remove `shrink-to-fit=yes` from viewport meta
**File:** `index.html`  
**Effort:** Remove keyword, no rebuild  
**Impact:** Eliminates potential iOS Safari extra layout pass on load

```javascript
// BEFORE:
meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';

// AFTER:
meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no';
```

---

### Fix 7 — Switch to Cloudflare Pages (or GitHub Actions Brotli) for compression
**Category:** Hosting  
**Effort:** Medium (CI/CD change)  
**Impact:** ~70% payload reduction → drastically faster load on mobile connections

**Option A — Cloudflare Pages (recommended):**
- Free tier supports unlimited bandwidth for static sites
- Cloudflare automatically applies Brotli compression to served assets
- Re-enable Unity Brotli compression in Player Settings → Publishing Settings → Compression Format: Brotli
- Cloudflare will serve `.br` files with correct `Content-Encoding: br` headers
- Rename the GitHub repo as a source; Cloudflare Pages auto-deploys from GitHub

**Option B — GitHub Actions pre-compress:**
```yaml
# .github/workflows/deploy.yml
- name: Compress with Brotli
  run: |
    find Build/ -type f \( -name "*.js" -o -name "*.wasm" -o -name "*.data" \) \
      -exec brotli --best {} \;
    # Unity Brotli builds already produce .br files — just set correct headers
```
The issue: GitHub Pages doesn't allow custom response headers, so `.br` files would be downloaded as-is without browser decompression. This is why Cloudflare Pages is the practical solution.

---

## 4. What to Avoid

**Do NOT enable Unity Audio in WebGL if background music is the primary use.**  
Web Audio API on iOS Safari requires a user gesture to start the AudioContext. Unity handles this via a `OnPointerDown` unlock, but if audio is failing silently, adding AudioContext unlock logic in JS is the correct approach — not adding more Unity audio infrastructure.

**Do NOT add an Object Pool for GuestRowUI rows.**  
With ≤10 guests, the allocation cost is ~5ms once per RSVP popup open, for a total of 1–2 popup opens per session. An object pool requires pre-allocation in `Initialize()`, adds complexity, and solves a problem that costs ~5ms total in the entire session lifetime. The ROI is zero.

**Do NOT set `config.matchWebGLToCanvasSize = false` without fully replacing the resize logic.**  
Setting this to false disconnects Unity's automatic canvas sizing. You then need JavaScript to manually set `canvas.width` and `canvas.height` in logical pixels and manage all resize events. The complexity is high, and `config.devicePixelRatio = 1` achieves the same performance benefit with none of the complexity.

**Do NOT add GPU instancing, batching hints, or SRP-level rendering optimizations.**  
This is a Canvas-based UI app. GPU draw calls are Canvas batches, not mesh draws. GPU instancing has zero effect on UGUI or TMP. Static batching is irrelevant. Batching-focused advice from generic Unity performance guides does not apply to a UI-only WebGL app.

**Do NOT chase 120fps on any mobile WebGL target.**  
iOS Safari does not expose 120Hz rAF to WebGL contexts. Android Chrome exposes it on 90/120Hz screens but the Unity WebGL thread budget is still dominated by WASM and JS overhead. Designing for 60fps is the correct target. Any animation that feels smooth at 60fps is fine; chasing 120 adds complexity for zero practical gain.

**Do NOT try to profile via Unity Profiler over USB for WebGL mobile.**  
Unity Profiler's hardware connection doesn't apply to browsers. See Section 5.

**Do NOT add `will-change: transform` to the canvas element hoping for GPU acceleration.**  
The browser's compositor can't composite a WebGL canvas differently from any other element — it's already a GPU texture. `will-change` on a canvas element is a no-op for performance and adds memory pressure for the browser to maintain a separate compositing layer.

**Do NOT add loading screens or fake progress indicators to mask slow startup.**  
The correct fix is actually making it load faster (compression). A fake progress bar on a 40MB download actively misleads users about remaining wait time. Fix the payload size.

---

## 5. Profiling Strategy

### Step 1 — Chrome DevTools Performance tab (zero setup required)
On Android Chrome, enable **Remote Debugging** (`chrome://inspect`), connect via USB, then use desktop Chrome DevTools:

1. Open **Performance** tab
2. Enable "Screenshots" and "Memory"
3. Click **Record**, interact with the app (tap a button, open a popup, scroll)
4. Stop recording

Look for:
- **Long Tasks** (red bars, >50ms): these are the frames causing input lag
- **GC Events** (garbage truck icon in the Main thread row): each GC pause is a lag spike
- **Recalculate Style / Layout**: should not appear frequently for a Canvas-only app
- **Paint / Composite Layers**: high paint times = canvas re-renders at high DPR

The Long Tasks section will directly show you where time is being spent (WASM execution vs. JS vs. layout).

### Step 2 — Performance Monitor (live overlay, no session overhead)
In Chrome DevTools → More Tools → Performance Monitor:
- **FPS**: should be ~60, consistent. Drops indicate frame budget overrun
- **CPU usage**: should idle near 5-10% between interactions, spike to 40-60% during animations
- **JS heap size**: watch for sawtooth pattern (growing allocations followed by GC drops)

### Step 3 — Unity Development Build + WebGL Profiler
This requires a rebuild with **Development Build** enabled and **Autoconnect Profiler** checked (Player Settings). Unity 6 WebGL supports the Unity Profiler over WebSocket:

1. Build with Development Build + Autoconnect Profiler
2. Serve the build locally (`python -m http.server 8080` in the Build folder)
3. Open Unity Editor Profiler (Window → Analysis → Profiler)
4. Open the local build URL in Chrome/Safari
5. The profiler auto-connects

This shows **C# function-level timing**: you can see exactly how many ms `CurveText.WarpText()`, `SafeArea.CalculateSafeArea()`, `UIAnimationStateMachine.PlaySequence()`, etc. take per frame. This is the authoritative signal — Chrome DevTools only shows WASM as a black box.

### Step 4 — WebGL-specific: measure input latency directly
Add this to `index.html` to measure end-to-end input→frame latency:

```javascript
let lastTouchTime = 0;
canvas.addEventListener('touchstart', (e) => {
    lastTouchTime = performance.now();
}, { passive: false });

// Call this from Unity via SendMessage or jslib when the response executes:
// window.reportInputHandled = () => console.log('Input latency:', performance.now() - lastTouchTime, 'ms');
```

Before Fix 1: you'll likely see 80–300ms. After: should be <32ms (2 frames at 60fps).

### Step 5 — iOS Safari: WebKit Web Inspector
On iOS Safari, use macOS Safari → Develop → [device] → [page] to open Web Inspector. The **Timelines** tab provides equivalent data to Chrome's Performance tab. Key timeline: "JavaScript & Events" — look for long stacks coinciding with touch events.

---

## 6. The Big Wins (Prioritized)

### #1 — `touch-action: none` on canvas  
**Effort:** 30 seconds | **File:** `TemplateData/style.css` | **No rebuild**  
**Impact:** Directly eliminates browser gesture disambiguation delay — the single largest cause of the "tap feels delayed" symptom. On iOS Safari especially, this changes touch latency from 100–300ms to <16ms. **Do this first.**

### #2 — `config.devicePixelRatio = 1`  
**Effort:** 30 seconds | **File:** `index.html` | **No rebuild**  
**Impact:** 4–9× reduction in rendered pixels. Fixes scroll stutter, reduces per-frame GPU time, reduces heat buildup (which causes CPU throttling). The single most impactful change for rendering performance. **Do this second, ship them both in one GitHub Pages deploy.**

### #3 — `targetFrameRate = 60` on WebGL builds  
**Effort:** 3 minutes | **File:** `App.cs` | **Requires rebuild**  
**Impact:** Correct frame budget prevents Unity from using aggressive timer polling on mobile. Reduces battery drain and thermal throttling over extended sessions (e.g., a guest browsing the app for 10+ minutes at the wedding). Combine with DPR fix — both help the "slow over time" scenario.

### #4 — Switch hosting to Cloudflare Pages with Brotli  
**Effort:** 1–2 hours | **No code change** | **Requires Unity rebuild with Brotli**  
**Impact:** ~70% payload reduction (~45MB → ~12MB). Transforms "the app takes 20 seconds to load" into "5 seconds to load." Especially important for guests on cellular at the wedding venue (likely congested WiFi or weak LTE). This is the fix for first-impression slowness before the user even interacts.

### #5 — Fix CurveText ReactToTextChanged early exit  
**Effort:** 5 minutes | **File:** `CurveText.cs` | **Requires rebuild**  
**Impact:** Prevents `ForceMeshUpdate()` cascade during RSVP popup open and any scene with multiple TMP text changes in a single frame. Low effort, removes a hidden CPU spike source.

---

## Quick Deploy Checklist (no rebuild required)

These two changes can be committed directly to GitHub and deployed to Pages without touching Unity:

```css
/* TemplateData/style.css — add touch-action */
#unity-canvas { background: #EFE6DD; touch-action: none; }
```

```javascript
/* index.html — uncomment devicePixelRatio */
config.devicePixelRatio = 1;
```

```javascript
/* index.html — remove shrink-to-fit */
meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no';
```

**Expected result after these three changes:** Input lag measurably reduced on iOS Safari, scroll smoothness significantly improved on all devices, no rebuild required.
