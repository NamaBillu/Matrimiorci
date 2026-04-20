# TESTING.md — Test Structure & Practices

## Framework

- **Package:** `com.unity.test-framework` v1.6.0 (Unity Test Runner — NUnit-based)
- **Test Runner:** Accessible via Unity Editor → Window → General → Test Runner

## Current Test Coverage

**No test files found** in the current codebase. The `com.unity.test-framework` package is installed but no tests have been authored yet.

Searched locations (standard Unity test directories):
- `Assets/Tests/` — not present
- `Assets/Editor/Tests/` — not present
- Any `*Test*.cs` or `*Spec*.cs` files — none found

## Unity Test Runner Overview (for future tests)

Unity Test Runner supports two test modes:

### EditMode Tests
- Run in the Unity Editor without entering Play mode
- Suitable for: pure logic, data structures, utility functions
- Location: `Assets/Tests/EditMode/` (must be in an assembly with `Editor` platform)

### PlayMode Tests
- Run with full Unity runtime (MonoBehaviour lifecycle, coroutines, etc.)
- Suitable for: Manager behaviors, scene loading, audio, popup state machines
- Location: `Assets/Tests/PlayMode/`

## Recommended Test Areas (Based on Current Code)

### High-Value Unit Tests (EditMode)
| Target | What to Test |
|--------|-------------|
| `SingletonComponent<T>` | Duplicate destruction, DontDestroyOnLoad only at root |
| `Logger.Log()` | Only logs in debug builds |
| `SafeArea.CalculateSafeArea()` | Anchor calculations from screen dimensions |
| `SoundManager` ID lookup | `GetSoundInfo` returns correct item or null |
| `PopupManager` ID lookup | Returns correct popup or logs error |

### Integration Tests (PlayMode)
| Target | What to Test |
|--------|-------------|
| `PopupManager.Show()` → `Popup.Show()` | Popup transitions to Showing state |
| `Popup.Hide()` → `OnSequenceCompleted` | Popup deactivates after hide animation |
| `SoundManager.Play()` | AudioSource created and playing |
| `SoundManager.SetSoundTypeOnOff()` | Stops correct sound type, saves to PlayerPrefs |
| `App.Awake()` | Screen settings applied correctly |

## Mocking

No mocking framework observed. Unity Test Framework supports:
- Substituting dependencies by swapping scene GameObjects
- Using `[UnitySetUp]` / `[UnityTearDown]` for scene state management
- For pure C# logic, standard NUnit mocking patterns apply (e.g., interface-based)

Note: Current codebase is tightly coupled to Unity (MonoBehaviour, static `Instance`) — mocking would require extracting interfaces or using Unity's `GameObject` test setup.

## Test Assembly Setup

To add tests, create assembly definition files (`.asmdef`):

```
Assets/Tests/
├── EditMode/
│   ├── EditModeTests.asmdef   (references: UnityEngine.TestRunner, UnityEditor.TestRunner)
│   └── *.Tests.cs
└── PlayMode/
    ├── PlayModeTests.asmdef   (references: UnityEngine.TestRunner)
    └── *.Tests.cs
```

## CI/CD

No CI configuration files (`.github/workflows/*.yml`, `Jenkinsfile`, etc.) observed. Test execution appears to be manual via Unity Editor.
