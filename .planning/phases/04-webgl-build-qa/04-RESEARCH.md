# Phase 04: WebGL Build & QA — Research

**Date:** April 20, 2026
**Phase:** 04-webgl-build-qa
**Requirements:** TECH-01 through TECH-08

---

## Executive Summary

Phase 4 is fundamentally different from Phases 1–3: it is almost entirely Unity Editor configuration, a build, and manual device testing — not C# code. The planner must produce checkpoint-heavy plans that walk the developer through exact Editor settings, a build command, GitHub Pages deployment, and a structured QA checklist.

Most steps **cannot be automated** by an agent (build triggering, device testing, Unity Editor UI interaction). Plans should provide exact setting paths and values, then use `checkpoint:human-action` or `checkpoint:human-verify` tasks.

---

## Domain 1: Unity 6 WebGL Build Settings

### Compression (TECH-01 — Gzip locked per D-01)

Location: **File → Build Settings → Player Settings → Publishing Settings → Compression Format**

Set to: **Gzip**

Unity 6 creates:
- `Build/<name>.data.gz`
- `Build/<name>.framework.js.gz`
- `Build/<name>.loader.js` (uncompressed — the entry point)
- `Build/<name>.wasm.gz`

The generated loader JS handles decompression client-side via JavaScript. GitHub Pages serves these files as `application/octet-stream` — this is fine because Unity's loader explicitly decompresses them in the browser, not relying on HTTP `Content-Encoding`.

Do NOT use Brotli with GitHub Pages — GitHub Pages does not send `Content-Encoding: br` headers for `.br` files, causing the browser to treat them as corrupt binary data.

### Memory Heap (TECH-03 — 32MB)

Location: **Player Settings → WebGL → Memory Size (MB)**

Set to: **32**

In Unity 6 (and Unity 2022+), WebGL uses a growable heap model. The "Memory Size" sets the initial allocation, not a hard cap. Default in Unity 6 is 32MB — but verify it hasn't been changed. Keeping at 32MB is correct for mobile browsers (iOS Safari has aggressive memory pressure; large initial heaps increase crash risk on low-memory devices).

### WebAssembly 2023 + BigInt (TECH-04)

Location: **Player Settings → WebGL → Target → WebAssembly 2023**

Enable: ✅ WebAssembly 2023

BigInt is part of the WebAssembly 2023 feature set and is enabled automatically when WebAssembly 2023 is checked. Unity 6 defaults to this enabled — verify it is not disabled.

**Browser support:** All target browsers (Chrome, Firefox, Edge, Safari 16.4+) support WebAssembly 2023. iOS 16.4+ required (covers all modern iPhones).

### Exception Handling

Location: **Player Settings → WebGL → Other Settings → Enable Exceptions**

Recommendation: Set to **None** for production builds — reduces WASM size significantly. No exception details visible, but this is a wedding app, not a debug build.

### Managed Code Stripping

Location: **Player Settings → Other Settings → Managed Stripping Level**

Recommendation: **Medium** or **High** — reduces build size. Test that stripping doesn't remove needed reflection-based code (DOTween uses reflection; check DOTween's Unity integration guide for stripping compatibility — `link.xml` may be needed if stripping breaks DOTween).

DOTween ships a `link.xml` file that preserves its required types. Verify `Assets/Plugins/Demigiant/DOTween/link.xml` (or similar) exists and is included.

---

## Domain 2: Audio Compression (TECH-05 — CompressedInMemory)

### Why This Matters for iOS

iOS enforces a hardware "Silent Mode" switch. When a device is in silent mode, Unity's WebGL audio fails **silently** (no error, just no sound) unless AudioClips are set to `CompressedInMemory`. This load type bypasses the iOS audio session issue.

### How to Set It

For each AudioClip asset in the project:
1. Select the AudioClip in the Project window
2. Inspector → **Load Type: Compressed In Memory**
3. Apply

This applies to ALL clips — background music AND sound effects.

### Finding All AudioClips

```bash
find Assets/ -name "*.mp3" -o -name "*.wav" -o -name "*.ogg" -o -name "*.aiff" 2>/dev/null
```

Also searchable via Unity's Project window with filter: `t:AudioClip`

### Alternative: Editor Script

An Editor script can batch-set all AudioClip import settings. This is automatable if clips are found:

```csharp
// Editor/SetAudioCompression.cs
using UnityEditor;
using UnityEngine;

public class SetAudioCompression
{
    [MenuItem("Tools/Set All Audio to CompressedInMemory")]
    static void SetAll()
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            Debug.Log($"Set CompressedInMemory: {path}");
        }
        Debug.Log("All AudioClips updated.");
    }
}
```

Run via: **Tools → Set All Audio to CompressedInMemory**

---

## Domain 3: Build Size (TECH-02 — ≤ 12MB)

### Realistic Unity 6 WebGL Size

A minimal Unity 6 WebGL project with UGUI, TMP, DOTween, and a small codebase typically compresses to 8–12MB with Gzip. The main contributors:

| Component | Approximate Compressed Size |
|-----------|----------------------------|
| Unity runtime (WASM) | 6–8 MB |
| Assets (textures, audio) | 1–3 MB |
| Managed code (C#) | ~0.5 MB |
| TMP fonts | ~0.3 MB |

**Key reduction strategies:**
1. Strip unused shaders (Graphics Settings → Always Included Shaders — remove unused)
2. Compress textures (ensure no uncompressed textures — check Import Settings)
3. Strip unused Unity modules (verify `ProjectSettings/ProjectSettings.asset` `m_EnabledVRDevices` is empty, etc.)
4. High managed code stripping (see Domain 1)
5. Disable stack trace for builds (Player Settings → Other Settings → Stack Trace: None)

**Checking size after build:**
- Unity prints total size in the Console after build completes
- Gzipped `.data.gz` + `.wasm.gz` + `.js.gz` combined = effective download size

---

## Domain 4: GitHub Pages Deployment

### Repository Setup

Options for GitHub Pages source:
1. **`gh-pages` branch (recommended)** — Build output goes to a separate branch, keeping `main` clean
2. **`/docs` folder on `main`** — Build output committed inside a `/docs` folder on the main branch

Recommended: **`gh-pages` branch** — cleaner separation between source code and build output.

### The `.nojekyll` File (CRITICAL)

GitHub Pages runs Jekyll by default, which **ignores files and directories starting with underscore**. Unity WebGL outputs files in a folder called `Build/` which may contain files with underscore-prefixed names depending on Unity version. More importantly, Unity 6 WebGL outputs a `StreamingAssets/` folder that Jekyll would process incorrectly.

Fix: create an empty `.nojekyll` file in the root of the deployed content.

```bash
touch .nojekyll
```

This disables Jekyll entirely for the Pages site. Without this, Unity WebGL may fail to load assets.

### Deployment Steps

**Manual deployment:**
1. Run Unity build → output to `WebGLBuild/` folder
2. Copy build output to a local `gh-pages` clone or worktree
3. Add `.nojekyll` in the root
4. Commit and push to `gh-pages` branch
5. In GitHub repo Settings → Pages: Source = `gh-pages` branch, folder = `/(root)`
6. Wait ~1 minute for Pages to publish

**Automated deployment (GitHub Actions alternative):**
Can be set up with a `.github/workflows/deploy.yml` using `peaceiris/actions-gh-pages@v3` action. This is optional but useful for re-deploying after content changes.

### MIME Type Notes

GitHub Pages serves unknown extensions with `application/octet-stream`. Unity's Gzip build files (`.data.gz`, `.wasm.gz`, `.framework.js.gz`) are served this way. This is correct behavior — Unity's loader JS decompresses them in the browser.

No `.htaccess` or custom headers are needed.

### HTTPS

GitHub Pages provides HTTPS automatically for all `*.github.io` domains. Custom domains require an additional CNAME record. No custom domain mentioned — use default `https://{user}.github.io/{repo}`.

---

## Domain 5: iOS Safari Compatibility

### Known Unity WebGL + iOS Safari Issues

| Issue | Unity 6 Status | Mitigation |
|-------|---------------|------------|
| AudioContext locked by default | Requires user gesture | Unity handles this — first tap unlocks audio. Already built into Unity WebGL template. |
| WebGL context loss on backgrounding | May occur | Unity restores context automatically. Acceptable behavior. |
| Safe area / notch overlap | ✅ Handled | `SafeArea.cs` already implemented in Phase 1. |
| Silent Mode audio failure | Must use CompressedInMemory | See Domain 2 above. |
| Large initial heap crash | 32MB is safe | See Domain 1. |
| Viewport height includes address bar | Affects full-height layouts | SafeArea handles this; UGUI Canvas Scaler set to Screen Space - Overlay is fine. |
| Input System touch support | ✅ New Input System | Already using `com.unity.inputsystem 1.17.0` — touch works. |
| WebAssembly 2023 | Requires iOS 16.4+ | All modern iPhones support this. Acceptable requirement. |

### iOS QA Specific Checks

1. App loads and shows splash / loading bar
2. Tap to unlock audio (first interaction) — sound plays correctly after
3. Invite scene displays correctly (fonts, colors, layout)
4. "Entra" button navigates to Home
5. Password entry keyboard appears on tap of code input field
6. Valid code unlocks gated sections
7. RSVP form — each input field opens keyboard, usable on iPhone screen
8. Submit RSVP — spinner shows, confirmation received
9. Rotated back to previous session correctly (PlayerPrefs persists)
10. Safe area respected on notched/Dynamic Island iPhone

---

## Domain 6: Android Chrome Compatibility

Android Chrome has excellent Unity WebGL support. Key checks:

1. App loads without crash
2. Touch gestures work (tap, scroll in popups)
3. Keyboard appears for text inputs
4. Audio plays correctly
5. QR code URL opens app correctly in Chrome (not downloaded as file)

**QR code note:** Some Android QR scanners open links in a built-in browser rather than Chrome. Recommend testing with Google Lens (opens Chrome) and the native camera app.

---

## Domain 7: QR Code

### URL Structure

Physical invite QR → `https://{user}.github.io/{repo}/?type=invite`

Default (from SMS/WhatsApp link, no param) → `https://{user}.github.io/{repo}/`

The trailing slash is important for GitHub Pages subpath projects — some QR scanners strip query params if the path doesn't end with `/` first. Test this.

### Generation

QR code generator: any free tool (e.g., qr-code-generator.com, QR Code Monkey). Generate as SVG/PNG for print at ≥300dpi.

The planner should not include QR generation in plans — this is a manual step the developer/couple handles outside the codebase.

---

## Domain 8: Build Configuration Reference (Unity 6 WebGL Player Settings)

Full list of settings to verify/change for this phase:

| Setting | Location | Target Value |
|---------|----------|-------------|
| Compression Format | Publishing Settings | Gzip |
| Initial Memory Size | WebGL | 32 |
| WebAssembly 2023 | WebGL | Enabled |
| BigInt | WebGL (auto with Wasm 2023) | Enabled |
| Enable Exceptions | WebGL | None |
| Managed Stripping Level | Other Settings | Medium |
| Stack Trace | Other Settings | None |
| Splash Screen (Unity logo) | Splash Screen | Already disabled by developer |
| AudioClip Load Type | Per-clip Import Settings | Compressed In Memory |

---

## Domain 9: Standard Stack

| Task | Tool | Notes |
|------|------|-------|
| WebGL build | Unity Editor UI | Cannot be triggered from command line without Unity license CLI |
| GitHub Pages deploy | `git push origin gh-pages` | Manual push after build |
| Audio batch set | Editor script (C#) | Can be automated — see Domain 2 |
| QA testing | Physical devices | No simulators |
| Build size check | Unity Console output | Logged after build |

---

## Don't Hand-Roll

- **Do not** write a custom web server or `nginx.conf` — GitHub Pages handles hosting
- **Do not** write a custom compression script — Unity's built-in Gzip is correct
- **Do not** use `WWW` or `WebGL Server` Unity package — not needed
- **Do not** write a custom iOS audio workaround — CompressedInMemory + Unity's WebGL template handles this

---

## Common Pitfalls

| Pitfall | Prevention |
|---------|-----------|
| Missing `.nojekyll` → Unity assets 404 | Always include in gh-pages root |
| Brotli on GitHub Pages → corrupt content | Use Gzip only (already locked D-01) |
| Too-large heap → iOS OOM crash | Keep at 32MB (TECH-03) |
| Audio silent on iOS | CompressedInMemory on ALL clips (TECH-05) |
| DOTween stripped out → NullRef at runtime | Verify `link.xml` exists in DOTween plugin folder |
| QR code without trailing slash → strip query param | Test QR URL on device before printing |
| Jekyll stripping `_framework` folder | `.nojekyll` file required |

---

## Architectural Responsibility Map

| Concern | Where It Lives |
|---------|---------------|
| Compression setting | Unity Player Settings (Publishing Settings) |
| Memory setting | Unity Player Settings (WebGL) |
| WebAssembly 2023 | Unity Player Settings (WebGL) |
| Audio load type | Per-clip AudioImporter settings (Editor script automates this) |
| `.nojekyll` | gh-pages branch root (file to create) |
| Pages source config | GitHub repo Settings → Pages (manual, in browser) |
| Device QA | Physical devices (manual developer action) |

---

## Phase 4 Plan Structure Recommendation

Given that this phase is mostly Editor configuration and manual steps, recommended plan breakdown:

**Plan 04-01 — Build Configuration** (Wave 1)
- Task 1: Editor script to batch-set all AudioClips to CompressedInMemory (automatable code)
- Task 2: Verify/document all Player Settings changes required (checkpoint:human-action — developer applies in Unity Editor)

**Plan 04-02 — Build + Deploy** (Wave 2, depends on 04-01)
- Task 1: Build WebGL from Unity Editor, verify size ≤ 12MB (checkpoint:human-action)
- Task 2: Set up gh-pages branch with .nojekyll, push build, configure Pages (automatable git steps + checkpoint:human-action for GitHub Pages settings UI)

**Plan 04-03 — QA** (Wave 3, depends on 04-02)
- Task 1: Structured QA checklist for Windows browsers (checkpoint:human-verify)
- Task 2: Structured QA checklist for Android + iOS physical devices (checkpoint:human-verify)
