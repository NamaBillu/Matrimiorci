# ARCHITECTURE.md — System Architecture

## Pattern

**Unity Singleton Manager Architecture** — A mobile UI app organized around persistent singleton MonoBehaviour managers, scene-based navigation, and a popup overlay system driven by UIAnimationStateMachine.

## Architectural Layers

```
┌──────────────────────────────────────────────────┐
│                    Scenes                         │
│   Bootloader  ──►  Home  ──►  Invite              │
└────────────────────┬─────────────────────────────┘
                     │ SceneManager.LoadSceneAsync
┌────────────────────▼─────────────────────────────┐
│              Persistent Managers                  │
│  App  ·  PopupManager  ·  SoundManager            │
│  (DontDestroyOnLoad Singletons)                   │
└────────────────────┬─────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────┐
│               UI / Popup Layer                    │
│  Popup (base)  ──►  FullScreenPopup (prefab)      │
│  UIAnimationStateMachine drives show/hide         │
└────────────────────┬─────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────┐
│                  Utilities                         │
│  SingletonComponent<T>  ·  SafeArea               │
│  IgnoreSafeArea  ·  Logger  ·  ButtonSound        │
└──────────────────────────────────────────────────┘
```

## Core Patterns

### Singleton Manager Pattern
All managers inherit `SingletonComponent<T>` which:
- Stores static `Instance` reference
- Destroys duplicate instances on Awake
- Calls `DontDestroyOnLoad` if the object has no parent (ensuring persistence across scenes)

```csharp
// Base class: Assets/App/Scripts/Utils/SingletonComponent.cs
public class SingletonComponent<T> : MonoBehaviour
{
    public static T Instance;
    protected virtual void Awake()
    {
        if (Instance == null) Instance = gameObject.GetComponent<T>();
        else { Destroy(gameObject); return; }
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }
}
```

### ID-Based Lookup
Both `PopupManager` and `SoundManager` use string ID registration:
- Popups and sounds are pre-registered via Inspector `List<...Info>` 
- Runtime retrieval via `Find(x => x.id == id)`
- Errors logged via `Debug.LogErrorFormat` when ID not found

### UIAnimationStateMachine (Show/Hide)
`Popup.cs` drives show/hide through two `UIAnimationStateMachine` references:
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
ButtonSound.PlaySound()
  → SoundManager.Instance.Play(soundId)
    → GetSoundInfo(id) [List lookup]
    → CreateAudioSource(id) [new GameObject + AudioSource]
    → audioSource.Play()
    → tracked in playingAudioSources list
    → Update() cleans up finished sources
```

### Popup Show
```
PopupManager.Show(id)
  → GetPopupById(id)
  → popup.Show(inData, callback)
    → UIAnimationStateMachine.PlaySequence() [show animation]
    → OnShowing(inData) [virtual, override in subclass]
  → popup.Transform.SetAsLastSibling() [bring to top]
  → activePopups.Add(popup)
```

### Popup Hide
```
popup.Hide(cancelled)
  → callback(cancelled, outData)
  → hideAnimationStateMachine.PlaySequence(true)
  → OnSequenceCompleted → state = Hidden, SetActive(false)
  → OnHiding() → PopupManager.OnPopupHiding(this)
```

## Abstractions

| Abstraction | Type | Purpose |
|-------------|------|---------|
| `SingletonComponent<T>` | Generic MonoBehaviour | Reusable singleton lifetime management |
| `Popup` | Abstract-like MonoBehaviour | Base class for all popup types with lifecycle hooks |
| `SafeArea` | Utility MonoBehaviour | Wraps screen safe area into RectTransform anchors |
| `Logger` | Static utility | Debug-build-only log wrapper |
