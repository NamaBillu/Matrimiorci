# CONVENTIONS.md — Code Style & Patterns

## Language & Style

- **Language:** C# (Unity-managed .NET)
- **Style:** Unity-idiomatic — MonoBehaviour lifecycle methods, Inspector serialization, `GetComponent<T>()`
- **No formatter config** observed (no `.editorconfig`, no StyleCop) — likely relies on IDE defaults (Rider/VS)

## Code Organization Pattern

All manager/component classes follow a consistent **`#region` block structure**:

```csharp
public class SoundManager : SingletonComponent<SoundManager>
{
    #region Classes          // Nested data classes / serializable structs
    #region Enums            // Public/private enums
    #region Inspector Variables   // [SerializeField] fields
    #region Member Variables      // Private non-inspector fields
    #region Properties       // Public auto/computed properties
    #region Unity Methods    // Awake, Start, Update, OnEnable, etc.
    #region Public Methods   // Public API
    #region Private Methods  // Internal logic
    #region Save Methods     // Persistence helpers
}
```

**This region structure should be followed for all new scripts.**

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

```csharp
// Inspector-exposed fields always use [SerializeField] private
[SerializeField] private List<SoundInfo> soundInfos = null;
[SerializeField] private string defaultThemeId;

// Inspector fields with validation hints use [Tooltip] and [Range]
[Tooltip("The popups id, used to show the popup.")]
public string popupId = "";
[Range(0, 1)] public float clipVolume = 1;
[Range(0f, 3f)] public float minVarPitch = 0f;

// Private runtime state — no attribute
private List<PlayingSound> playingAudioSources = new();
private bool isFocused = false;
```

## Singleton Access Pattern

All managers expose a static `Instance` via `SingletonComponent<T>`. Callers use null-check defensively:

```csharp
// ButtonSound.cs — defensive null check
if (SoundManager.Instance != null)
{
    SoundManager.Instance.Play(soundId);
}
```

Managers themselves use `Instance` directly without null checks (assumed always present).

## ID-Based Lookup Pattern

Both `PopupManager` and `SoundManager` register assets by string ID via Inspector lists:

```csharp
// Registration
[SerializeField] private List<SoundInfo> soundInfos = null;

// Lookup
private SoundInfo GetSoundInfo(string id) => soundInfos.Find(x => x.id == id);

// Error handling on miss
Debug.LogErrorFormat("[PopupController] Popup with id {0} does not exist", id);
```

## Error Handling

- **Missing IDs:** `Debug.LogErrorFormat` with context tag (e.g., `[PopupController]`)
- **Null guards:** Defensive null checks before using `Instance` references
- **No exceptions thrown** in observed code — Unity pattern of silent failure + log
- **Logger utility** wraps debug logging behind `Debug.isDebugBuild` check:

```csharp
// Assets/App/Scripts/Utils/Logger.cs
public static void Log(string message)
{
    if (Debug.isDebugBuild)
        Debug.Log(message);
}
```

## Unity Lifecycle Conventions

- `Awake()` — initialization, singleton setup, component caching
- `Start()` — post-initialization work (e.g., loading saved settings, starting coroutines)
- `Update()` — runtime polling (used in `SoundManager` to clean up finished audio sources)
- `protected override void Awake()` — always calls `base.Awake()` first when extending `SingletonComponent<T>`

## Coroutines

Used for delayed operations:

```csharp
// App.cs — safety delay before hiding loading screen
private IEnumerator SafeCheckOnGameLoaded()
{
    yield return new WaitForSeconds(3f);
    mainLoadingAnimator.gameObject.SetActive(false);
}
```

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

When removing items from a list during forward iteration:

```csharp
// Correct pattern used in SoundManager.cs
for (int i = 0; i < playingAudioSources.Count; i++)
{
    if (!audioSource.isPlaying)
    {
        Destroy(audioSource.gameObject);
        playingAudioSources.RemoveAt(i);
        i--;  // ← compensate for removed index
    }
}
```
