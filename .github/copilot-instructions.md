<!-- GSD Configuration — managed by get-shit-done installer -->
# Instructions for GSD

- Use the get-shit-done skill when the user asks for GSD or uses a `gsd-*` command.
- Treat `/gsd-...` or `gsd-...` as command invocations and load the matching file from `.github/skills/gsd-*`.
- When a command says to spawn a subagent, prefer a matching custom agent from `.github/agents`.
- Do not apply GSD workflows unless the user explicitly asks for them.
- After completing any `gsd-*` command (or any deliverable it triggers: feature, bug fix, tests, docs, etc.), ALWAYS: (1) offer the user the next step by prompting via `ask_user`; repeat this feedback loop until the user explicitly indicates they are done.
<!-- /GSD Configuration -->

<!-- GSD:project-start source:PROJECT.md -->
## Project

**Matrimiorci**

A Unity WebGL wedding app accessible from any browser (desktop and mobile). Guests arrive via QR code (physical invite) or a personalized digital link and find all wedding information in one place. A password system unlocks personalized content and RSVP functionality for each guest group.

**Core Value:** Every guest can access all wedding information and RSVP from a link, from any device, without installing anything.

### Constraints

- **Timeline:** 2 weeks — ship MVP, defer everything non-essential
- **Tech stack:** Unity 6 WebGL fixed — no switching engines
- **Budget:** Zero — all external services must be free tier forever at this scale
- **Browser target:** Desktop browsers + mobile browsers (iOS Safari, Android Chrome)
- **Scale:** <10 groups, <50 guests — no scalability optimization needed
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Runtime & Engine
| Layer | Technology | Version |
|-------|-----------|---------|
| Game Engine | Unity | 6000.3.5f2 (Unity 6) |
| Language | C# | .NET (Unity-managed) |
| Target Platform | Mobile (iOS/Android implied) | — |
| Scripting Backend | Mono (default) | — |
## Unity Packages (via Package Manager)
### First-Party Unity Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.ugui` | 2.0.0 | Unity UI (Canvas, RectTransform, Button, etc.) |
| `com.unity.inputsystem` | 1.17.0 | New Input System |
| `com.unity.timeline` | 1.8.10 | Timeline/Animation sequencing |
| `com.unity.visualscripting` | 1.9.9 | Visual Scripting (Bolt) |
| `com.unity.test-framework` | 1.6.0 | Unity Test Runner (NUnit-based) |
| `com.unity.feature.2d` | 2.0.2 | 2D feature set |
| `com.unity.multiplayer.center` | 1.0.1 | Multiplayer tooling hub |
| `com.unity.collab-proxy` | 2.11.2 | Unity Version Control (Plastic/DevOps) |
| `com.unity.ide.rider` | 3.0.38 | JetBrains Rider IDE integration |
| `com.unity.ide.visualstudio` | 2.0.26 | Visual Studio IDE integration |
### Third-Party Packages (via Git URL)
| Package | Source | Purpose |
|---------|--------|---------|
| `com.namabillu.colorpalette` | `github.com/NamaBillu/ColorPalette.git` | Color palette management |
| `com.namabillu.uianimations` | `github.com/NamaBillu/UI-Animations.git` | UI animation state machines (`UIAnimator`, `UIAnimationStateMachine`) |
### Plugins (Assets/Plugins/)
| Plugin | Location | Purpose |
|--------|----------|---------|
| DOTween | `Assets/Plugins/Demigiant/DOTween/` | Tween animation library |
## Key Unity Modules Enabled
- `com.unity.modules.animation` — Animator/Animation
- `com.unity.modules.audio` — AudioSource, AudioClip
- `com.unity.modules.physics2d` — 2D Physics
- `com.unity.modules.ui` — Core UI
- `com.unity.modules.uielements` — UI Toolkit
- `com.unity.modules.unitywebrequest` — HTTP networking
- `com.unity.modules.jsonserialize` — JsonUtility
- `com.unity.modules.imageconversion` — Texture/Image conversion
- `com.unity.modules.androidjni` — Android native bridge
- `com.unity.modules.screencapture` — Screen capture
## Asset Configuration
### Resources
- `Assets/Resources/DOTweenSettings.asset` — DOTween global config
- `Assets/Editor/ColorPalettes.json` — Color palette definitions
### Fonts (TextMesh Pro)
- `CrimsonText-Regular` — Serif font (body text)
- `Slight` — Custom/decorative font (display text)
- TextMesh Pro package is present via standard Unity TMP
### Input
- `Assets/InputSystem_Actions.inputactions` — New Input System action map
## Build & Project Settings
- **Target Frame Rate:** 120 FPS (set in `App.cs`)
- **VSync:** Disabled (`QualitySettings.vSyncCount = 0`)
- **Sleep Timeout:** `SleepTimeout.NeverSleep` (keeps screen on)
- **Unity Version Control:** `com.unity.collab-proxy` present (team collaboration)
## Solution Files
- `Assembly-CSharp.csproj` — Main game assembly
- `Assembly-CSharp-firstpass.csproj` — Plugin assembly
- `DOTween.Modules.csproj` — DOTween modules assembly
- `Matrimiorci.slnx` — Solution file
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Language & Style
- **Language:** C# (Unity-managed .NET)
- **Style:** Unity-idiomatic — MonoBehaviour lifecycle methods, Inspector serialization, `GetComponent<T>()`
- **No formatter config** observed (no `.editorconfig`, no StyleCop) — likely relies on IDE defaults (Rider/VS)
## Code Organization Pattern
## Naming Conventions
| Element | Convention | Examples |
|---------|-----------|---------|
| Classes | PascalCase | `PopupManager`, `SoundManager`, `SafeArea` |
| Interfaces | Not observed | — |
| Methods | PascalCase | `PlaySound()`, `OnShowing()`, `CalculateSafeArea()` |
| Private fields | camelCase | `activePopups`, `isFocused`, `defaultThemeId` |
| `[SerializeField]` fields | camelCase | `soundInfos`, `popupInfos`, `mainLoadingAnimator` |
| Public properties | PascalCase | `IsMusicOn`, `IsInitialized`, `CanAndroidBackClosePopup` |
| Enums (type) | PascalCase | `SoundType`, `State` |
| Enum values | PascalCase | `SoundEffect`, `Music`, `Shown`, `Hidden` |
| Nested data classes | PascalCase | `SoundInfo`, `PlayingSound`, `PopupInfo` |
| Constants / string IDs | lowercase string literals | `"IsMusicOn"`, `"IsSoundEffectsOn"` |
| Prefabs / Assets | PascalCase, spaces allowed | `HomeItem Variant.prefab` |
## Field Declaration Patterns
## Singleton Access Pattern
## ID-Based Lookup Pattern
## Error Handling
- **Missing IDs:** `Debug.LogErrorFormat` with context tag (e.g., `[PopupController]`)
- **Null guards:** Defensive null checks before using `Instance` references
- **No exceptions thrown** in observed code — Unity pattern of silent failure + log
- **Logger utility** wraps debug logging behind `Debug.isDebugBuild` check:
## Unity Lifecycle Conventions
- `Awake()` — initialization, singleton setup, component caching
- `Start()` — post-initialization work (e.g., loading saved settings, starting coroutines)
- `Update()` — runtime polling (used in `SoundManager` to clean up finished audio sources)
- `protected override void Awake()` — always calls `base.Awake()` first when extending `SingletonComponent<T>`
## Coroutines
## Events / Delegates
- `PopupClosed` delegate: `public delegate void PopupClosed(bool cancelled, object[] outData)`
- `SafeArea.SafeAreaChanged` — `Action<RectTransform>` event for safe area change notifications
- `UIAnimationStateMachine.OnSequenceCompleted` — UnityEvent used to react to animation completion
## Comments / Documentation
- **No XML doc comments** (`///`) observed
- **`[Tooltip]`** used on serialized Inspector fields
- **Inline comments** used sparingly for non-obvious logic
- **Commented-out code blocks** are present (loading screen in `App.cs`, CanvasGroup in `Popup.cs`) — indicates work-in-progress sections
## List Iteration Safety
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern
## Architectural Layers
```
```
## Core Patterns
### Singleton Manager Pattern
- Stores static `Instance` reference
- Destroys duplicate instances on Awake
- Calls `DontDestroyOnLoad` if the object has no parent (ensuring persistence across scenes)
```csharp
```
### ID-Based Lookup
- Popups and sounds are pre-registered via Inspector `List<...Info>` 
- Runtime retrieval via `Find(x => x.id == id)`
- Errors logged via `Debug.LogErrorFormat` when ID not found
### UIAnimationStateMachine (Show/Hide)
- `showAnimationStateMachine.PlaySequence()` on show
- `hideAnimationStateMachine.PlaySequence(true)` on hide, with `OnSequenceCompleted` callback that deactivates the GameObject
### Scene Flow
- **Bootloader** scene — entry point, hosts persistent manager GameObjects (`App`, `PopupManager`, `SoundManager`)
- **Home** scene — main application screen
- **Invite** scene — invite/wedding invitation screen
- `App.cs` loads Home scene asynchronously from Bootloader (currently commented-out, loading screen infrastructure exists but is disabled)
## Entry Points
| Entry Point | File | Purpose |
|-------------|------|---------|
| App.Awake() | `Assets/App/Scripts/Managers/App.cs` | Configure screen settings, initialize app |
| App.Start() | `Assets/App/Scripts/Managers/App.cs` | Trigger initial scene load (commented out) |
| PopupManager.Awake() | `Assets/App/Scripts/Managers/PopupManager.cs` | Initialize all registered popups |
| SoundManager.Start() | `Assets/App/Scripts/Managers/SoundManager.cs` | Load sound settings from PlayerPrefs |
## Data Flow
### Sound Playback
```
```
### Popup Show
```
```
### Popup Hide
```
```
## Abstractions
| Abstraction | Type | Purpose |
|-------------|------|---------|
| `SingletonComponent<T>` | Generic MonoBehaviour | Reusable singleton lifetime management |
| `Popup` | Abstract-like MonoBehaviour | Base class for all popup types with lifecycle hooks |
| `SafeArea` | Utility MonoBehaviour | Wraps screen safe area into RectTransform anchors |
| `Logger` | Static utility | Debug-build-only log wrapper |
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.github/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->

