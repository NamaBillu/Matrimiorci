# STRUCTURE.md — Directory Layout & Organization

## Top-Level Layout

```
Matrimiorci/
├── Assets/                    ← All game/app content
│   ├── App/                   ← Main application code & assets
│   │   ├── Images/            ← Sprites and UI images
│   │   │   ├── Icons/         ← Icon sprites (currently empty)
│   │   │   └── UI/            ← UI graphics (Circle@{size}.png set)
│   │   ├── Prefabs/           ← Reusable GameObjects
│   │   │   ├── Popup/         ← Popup prefabs
│   │   │   │   └── FullScreenPopup.prefab
│   │   │   ├── HomeItem.prefab
│   │   │   ├── HomeItem Variant.prefab
│   │   │   └── SideMenuItem.prefab
│   │   ├── Scenes/            ← Unity scene files
│   │   │   ├── Bootloader.unity   ← App entry point
│   │   │   ├── Home.unity         ← Main screen
│   │   │   └── Invite.unity       ← Invite/wedding invitation screen
│   │   └── Scripts/           ← All C# source code
│   │       ├── Managers/      ← Singleton manager scripts
│   │       │   ├── App.cs
│   │       │   ├── PopupManager.cs
│   │       │   └── SoundManager.cs
│   │       ├── Popup/         ← Popup base class
│   │       │   └── Popup.cs
│   │       └── Utils/         ← Utility/helper scripts
│   │           ├── ButtonSound.cs
│   │           ├── IgnoreSafeArea.cs
│   │           ├── Logger.cs
│   │           ├── SafeArea.cs
│   │           └── SingletonComponent.cs
│   ├── Editor/                ← Editor-only scripts and data
│   │   └── ColorPalettes.json ← Color palette config for ColorPalette package
│   ├── Fonts/                 ← Font assets
│   │   ├── CrimsonText-Regular.ttf / .asset
│   │   └── Slight.otf / .asset
│   ├── Plugins/               ← Third-party plugins
│   │   └── Demigiant/DOTween/ ← DOTween tween library
│   ├── Resources/             ← Runtime-loaded assets
│   │   └── DOTweenSettings.asset
│   ├── TextMesh Pro/          ← TMP settings and fonts
│   └── InputSystem_Actions.inputactions ← Input action map
├── Packages/
│   ├── manifest.json          ← Package Manager dependencies
│   └── packages-lock.json     ← Locked package versions
├── ProjectSettings/           ← Unity project configuration
├── .planning/                 ← GSD planning artifacts
│   └── codebase/              ← Codebase map documents
└── .github/                   ← GSD skills, workflows, agents
```

## Scripts Directory Convention

Scripts are organized by role, not by feature:

```
Scripts/
├── Managers/   ← Singleton MonoBehaviours managing global state
├── Popup/      ← Base popup classes
└── Utils/      ← Stateless helpers, base classes, UI utilities
```

## Key File Locations

| File | Path | Purpose |
|------|------|---------|
| App entry / main manager | `Assets/App/Scripts/Managers/App.cs` | Scene routing, screen config |
| Popup system | `Assets/App/Scripts/Managers/PopupManager.cs` | Popup registration & show/hide |
| Audio system | `Assets/App/Scripts/Managers/SoundManager.cs` | Sound playback & settings |
| Popup base | `Assets/App/Scripts/Popup/Popup.cs` | Base class for all popups |
| Singleton base | `Assets/App/Scripts/Utils/SingletonComponent.cs` | Generic singleton pattern |
| Safe area | `Assets/App/Scripts/Utils/SafeArea.cs` | Screen safe area adapter |
| Safe area ignore | `Assets/App/Scripts/Utils/IgnoreSafeArea.cs` | Opt-out of safe area constraints |
| Debug logger | `Assets/App/Scripts/Utils/Logger.cs` | Debug-build-only logging |
| Button sound | `Assets/App/Scripts/Utils/ButtonSound.cs` | Attach-and-forget sound on button |
| Color palettes | `Assets/Editor/ColorPalettes.json` | Design system color definitions |
| Package manifest | `Packages/manifest.json` | All NPM-style Unity dependencies |

## Scenes

| Scene | File | Role |
|-------|------|------|
| Bootloader | `Assets/App/Scenes/Bootloader.unity` | App initialization, hosts persistent managers |
| Home | `Assets/App/Scenes/Home.unity` | Main app screen |
| Invite | `Assets/App/Scenes/Invite.unity` | Wedding invitation/sharing screen |

## Prefabs

| Prefab | Path | Notes |
|--------|------|-------|
| HomeItem | `Assets/App/Prefabs/HomeItem.prefab` | List/grid item for Home screen |
| HomeItem Variant | `Assets/App/Prefabs/HomeItem Variant.prefab` | Style variant of HomeItem |
| SideMenuItem | `Assets/App/Prefabs/SideMenuItem.prefab` | Navigation drawer item |
| FullScreenPopup | `Assets/App/Prefabs/Popup/FullScreenPopup.prefab` | Full-screen popup overlay |

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| C# classes | PascalCase | `PopupManager`, `SoundManager` |
| C# methods | PascalCase | `PlaySound()`, `OnShowing()` |
| Private fields | camelCase | `activePopups`, `soundInfos` |
| Properties | PascalCase | `IsMusicOn`, `IsInitialized` |
| Sound/popup IDs | string literals | `"defaultTheme"`, `"FullScreenPopup"` |
| Prefabs | PascalCase with spaces | `HomeItem Variant.prefab` |
| Scenes | PascalCase | `Bootloader.unity`, `Home.unity` |
| Unity Inspector variables | camelCase + `[SerializeField]` | `popupInfos`, `soundInfos` |
