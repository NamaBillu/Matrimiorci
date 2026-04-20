# STACK.md — Technology Stack

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
