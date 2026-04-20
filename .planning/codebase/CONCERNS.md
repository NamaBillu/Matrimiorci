# CONCERNS.md — Technical Debt, Issues & Risks

## Summary

This is an early-stage Unity 6 mobile app (wedding/matrimony theme). The codebase is small and relatively clean, but several patterns present risk as the project scales.

---

## High Priority

### 1. Commented-Out Core Loading Logic (App.cs)
**File:** `Assets/App/Scripts/Managers/App.cs`
**Issue:** The entire scene-loading flow in `Start()` is commented out. The app cannot transition from Bootloader to Home at runtime:

```csharp
// App.cs Start()
// if (SceneManager.GetActiveScene().name != homeSceneId)
// {
//     SceneManager.sceneLoaded += SceneManager_sceneLoaded;
//     SceneManager.LoadSceneAsync(homeSceneId);
// }
```

The `mainLoadingAnimator.gameObject.SetActive(true)` call is also commented. This suggests the Bootloader→Home flow is either unfinished or was temporarily disabled.

**Risk:** Application may not navigate correctly in production builds.

---

### 2. No Tests
**Issue:** `com.unity.test-framework` is installed but zero test files exist. All manager logic (sound, popup lifecycle, scene loading) is untested.
**Risk:** Regressions are invisible. Manual testing is the only safety net.

---

### 3. Public Static `Instance` (Not Encapsulated)
**File:** `Assets/App/Scripts/Utils/SingletonComponent.cs`
**Issue:** `Instance` is `public static T` — it can be set or overwritten from any class:

```csharp
public static T Instance;  // ← writable by anyone
```

**Risk:** Accidental reassignment could produce hard-to-debug null reference issues or cross-scene leaks.
**Fix:** Change to `public static T Instance { get; private set; }`.

---

## Medium Priority

### 4. String-Based ID System (No Compile-Time Safety)
**Files:** `PopupManager.cs`, `SoundManager.cs`, `ButtonSound.cs`
**Issue:** Popup and sound IDs are plain strings (`"defaultTheme"`, `"FullScreenPopup"`). Typos cause silent runtime failures — sounds don't play, popups don't open, error only appears in logs.
**Risk:** Refactoring IDs is error-prone. New developers won't know valid IDs without reading Inspector configuration.
**Fix:** Use `const` fields or an `enum` + extension method mapping.

---

### 5. AudioSource GameObject Leak Risk
**File:** `Assets/App/Scripts/Managers/SoundManager.cs`
**Issue:** Each call to `Play()` creates a new `GameObject` child (`"sound_" + id`). Cleanup relies on `Update()` polling `audioSource.isPlaying`:

```csharp
private AudioSource CreateAudioSource(string id)
{
    GameObject obj = new GameObject("sound_" + id);
    obj.transform.SetParent(transform);
    return obj.AddComponent<AudioSource>();
}
```

If `OnApplicationFocus`/`OnApplicationPause` edge cases cause `isPlaying` to return false prematurely (e.g., app backgrounded mid-sound), the source may be destroyed early.

For looping sounds (`loopingAudioSources`), cleanup requires explicit `Stop()` calls — if `Stop()` is never called, these objects persist indefinitely.
**Risk:** Memory leak with many looping sounds or if `Stop()` is missed.

---

### 6. `FindFirstObjectByType<SafeArea>()` in IgnoreSafeArea
**File:** `Assets/App/Scripts/Utils/IgnoreSafeArea.cs`
**Issue:** Uses `FindFirstObjectByType<SafeArea>()` in `Awake()`, which is a scene-wide search — O(n) over all objects:

```csharp
safeArea = FindFirstObjectByType<SafeArea>();
```

**Risk:** Performance cost scales with scene complexity. If `SafeArea` is not present in the scene, `safeArea` is null, causing NullReferenceException in `Start()`.

---

### 7. Large Commented-Out Block in Popup.cs
**File:** `Assets/App/Scripts/Popup/Popup.cs`
**Issue:** Large section of `CanvasGroup` code is commented out, alongside a commented-out `UIAnimator` accessor. The code suggests an older animation approach was replaced by `UIAnimationStateMachine`, but the old code was not removed.
**Risk:** Developer confusion about which animation path is canonical. Dead code increases maintenance burden.

---

## Low Priority

### 8. No .editorconfig / Code Style Enforcement
**Issue:** No `.editorconfig`, StyleCop, or Roslyn analyzer config is present.
**Risk:** Style drift over time, especially with multiple contributors.

### 9. Multiplayer Package Included Unused
**Package:** `com.unity.multiplayer.center` 1.0.1
**Issue:** Package is in `manifest.json` but no multiplayer code exists.
**Risk:** Increases project compilation overhead and package resolution time.

### 10. No Null Safety for PopupManager Active List
**File:** `Assets/App/Scripts/Managers/PopupManager.cs`
**Issue:** `activePopups.Add(popup)` is called in `Show()`, but `OnPopupHiding()` (which removes from the list) is not shown in the reviewed code. If a popup is destroyed without properly calling `Hide()`, the `activePopups` list may hold stale references.

### 11. PlayerPrefs as Only Persistence Mechanism
**File:** `Assets/App/Scripts/Managers/SoundManager.cs`
**Issue:** Settings are saved via `PlayerPrefs` which stores to platform-specific key-value storage. No encryption, no versioning, no migration strategy.
**Risk:** If key names change, saved settings are silently lost. Not suitable for sensitive data.

---

## Fragile Areas

| Area | File | Fragility |
|------|------|-----------|
| Scene loading flow | `App.cs` | Commented out — not functional |
| Popup animation completion | `Popup.cs` | Depends on `hideAnimationStateMachine.OnSequenceCompleted` firing; if animation is skipped or missing, popup never hides |
| Safe area on missing component | `IgnoreSafeArea.cs` | NullReferenceException if `SafeArea` not in scene |
| Sound cleanup | `SoundManager.cs` | Looping sounds leak if `Stop()` is never called |
| Singleton instance | `SingletonComponent.cs` | Public writeable `Instance` is error-prone |
