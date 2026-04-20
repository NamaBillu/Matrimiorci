# INTEGRATIONS.md — External Integrations

## External Packages (Git-based)

### NamaBillu/ColorPalette
- **URL:** `https://github.com/NamaBillu/ColorPalette.git?path=Assets/ColorPalette`
- **Namespace/Types:** `ColorPalette` (assumed)
- **Config:** `Assets/Editor/ColorPalettes.json` — palette definitions loaded at editor time
- **Usage:** Color theming/design system for the UI

### NamaBillu/UI-Animations
- **URL:** `https://github.com/NamaBillu/UI-Animations.git?path=Assets/UIAnimations`
- **Namespace/Types:** `UIAnimations` namespace — `UIAnimator`, `UIAnimationStateMachine`
- **Usage in codebase:**
  - `App.cs` — `UIAnimator mainLoadingAnimator` for loading screen transitions
  - `Popup.cs` — `UIAnimationStateMachine showAnimationStateMachine` / `hideAnimationStateMachine` for show/hide sequences
  - `OnSequenceCompleted` event on `hideAnimationStateMachine` drives popup state transitions

### DOTween (Demigiant)
- **Location:** `Assets/Plugins/Demigiant/DOTween/`
- **Config:** `Assets/Resources/DOTweenSettings.asset`
- **Usage:** Tween-based animations (included but specific usage outside of UIAnimations integration not observed in current scripts)

## Platform APIs

### Android
- `com.unity.modules.androidjni` present — Android JNI bridge enabled
- Likely target platform (mobile app context)

### iOS
- `SleepTimeout.NeverSleep` in `App.cs` targets mobile specifically
- Safe Area handling (`SafeArea.cs`, `IgnoreSafeArea.cs`) addresses iPhone notch/home indicator

## Persistence

### PlayerPrefs (Unity built-in)
- **Used by:** `SoundManager.cs`
- **Keys stored:**
  - `IsMusicOn` (int 0/1)
  - `IsSoundEffectsOn` (int 0/1)
- No other persistence layer (no SQLite, no cloud save) observed

## Networking

### UnityWebRequest
- `com.unity.modules.unitywebrequest` enabled in manifest
- No direct usage observed in current scripts — capability present but unused or planned

### Multiplayer
- `com.unity.multiplayer.center` 1.0.1 in manifest
- No multiplayer logic found in current scripts — possibly exploratory inclusion

## Unity Services

### Version Control
- `com.unity.collab-proxy` 2.11.2 — Unity Version Control (Plastic SCM / Unity DevOps) for team collaboration

### Analytics
- `com.unity.modules.unityanalytics` present in modules
- No analytics calls observed in current scripts

## External APIs
None identified in current script files. All data appears to be local/offline.

## Summary Table

| Integration | Type | Status |
|-------------|------|--------|
| NamaBillu UIAnimations | Git Package | Active — core UI animations |
| NamaBillu ColorPalette | Git Package | Active — color theming |
| DOTween | Plugin | Present — usage extent unclear |
| PlayerPrefs | Unity Built-in | Active — sound settings persistence |
| UnityWebRequest | Unity Module | Enabled, not yet used |
| Multiplayer Center | Unity Package | Included, not yet used |
| Unity Analytics | Unity Module | Enabled, not yet used |
