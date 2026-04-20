# Phase 2: Wedding Content - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers all wedding information displayed in Home and completes the Invite scene.
Code provides the **architecture** (section buttons, group-aware gating, data model extensions).
**UI layout, visual design, copy text, images, and FAQ content are handled in Editor by the developer** — not scripted.

</domain>

<decisions>
## Implementation Decisions

### D-01: Home Scene Layout Architecture

Home is a **scrollable Canvas** with section buttons/cards vertically stacked. A **burger menu button** opens a side menu (using `SideMenuItem.prefab`) replicating the same section list for quick access without closing popups. Developer handles all UI arrangement in Editor.

No tab navigation — single scrollable view.

### D-02: SectionButton Component

New `SectionButton` MonoBehaviour with:
- `[SerializeField] string popupId` — which popup to open when unlocked
- `[SerializeField] bool requiresUnlock = true` — whether this button is gated
- `[SerializeField] GameObject lockIcon` — GameObject shown when locked, hidden when unlocked
- `[SerializeField] string lockedHintText` — optional hint shown when locked (e.g., "inserisci il tuo codice")

**Click behavior:**
- If `requiresUnlock && !App.Instance.IsUnlocked` → open PasswordPopup via `PopupManager.Instance.Show("PasswordPopup")`
- If unlocked (or `requiresUnlock = false`) → open `popupId` popup via `PopupManager.Instance.Show(popupId)`

**State reaction:** Subscribe to `App.OnUnlocked` in `OnEnable`, unsubscribe in `OnDisable`. On unlock: hide lock icon, re-register click listener to open `popupId` popup.

**Locked section layout in Home scroll (per user description):**
```
- Section 1 button
- Section 2 button
- Section 3 button
--- "inserisci il tuo codice per visualizzare" ---
- Locked Section 1 button (lock icon visible, click → PasswordPopup)
- Locked Section 2 button (lock icon visible, click → PasswordPopup)
```

After unlock, locked sections hide their lock icon and click opens the real popup.

### D-03: Group-Specific Content Visibility — GroupContentGate

New `GroupContentGate` MonoBehaviour for content that should only appear for specific password groups.

- `[SerializeField] List<string> visibleToPasswords` — list of password strings (case-insensitive). **Empty = visible to ALL authenticated groups.** Populated = only groups whose password is in this list see the content.
- `[SerializeField] GameObject content` — the GameObject to show/hide

**Tiers this covers (per user):**
- `password/no-password` → handled by existing `ContentGate` (Phase 1, not this component)
- `all authenticated groups` → `GroupContentGate` with empty `visibleToPasswords` list
- `specific group subset` → `GroupContentGate` with 2+ passwords listed
- `one specific group only` → `GroupContentGate` with exactly 1 password listed

**Behavior:** Subscribe to `App.OnUnlocked` on `OnEnable`/`OnDisable`. On `Refresh()`: check `App.Instance.IsUnlocked` AND whether `App.Instance.CurrentGroup.password` (case-insensitive) is in `visibleToPasswords` (or list is empty). Show `content` if both conditions pass; hide otherwise.

### D-04: GroupData Extension for Per-Group Variant Content

Extend `GroupData` (existing `[Serializable]` class) with accommodation-specific fields for groups with reserved apartments:

```csharp
public bool hasApartment = false;          // true for groups with a reserved apartment
public string apartmentName = "";           // e.g. "Appartamento Gialli"
public string apartmentAddress = "";        // full address
public List<string> apartmentNotes = new List<string>(); // extra info (check-in time, key pickup, etc.)
```

`hasBreakfastPref` (Phase 1) covers breakfast; `hasApartment` covers accommodation detail display.

The developer fills all fields in the Unity Inspector on the App GameObject in Bootloader scene.

### D-05: Per-Group Popup Variant Strategy

Complex per-group variant popups (e.g., `AlloggioPopup`) are **custom `Popup` subclasses** — they read `App.Instance.CurrentGroup` in `OnShowing()` and populate their own `[SerializeField]` UI fields accordingly.

Example `AlloggioPopup` logic:
- If `CurrentGroup.hasApartment == true` → show apartment name/address/notes UI elements; hide generic suggestions list
- If `CurrentGroup.hasApartment == false` → show generic B&B/hotel suggestions; hide apartment-specific UI

Developer wires all UI references in Inspector. Script only drives show/hide and text population.

### D-06: Invite Scene

Invite scene already exists. **Developer handles all UI and design in Editor** — scrollable display of physical invitation images (without QR code), plus a button at the bottom to navigate to Home (`SceneManager.LoadScene(homeSceneId)` or equivalent).

**Code deliverable:** None needed beyond what Phase 1 already built for routing. If a button script is needed, it's a simple `[SerializeField] string sceneId` + `SceneManager.LoadScene(sceneId)` component — or just wired directly in Editor using existing scene load methods.

### Agent's Discretion

- Exact field names and region structure in `SectionButton.cs` and `GroupContentGate.cs` — follow CONVENTIONS.md `#region` block structure
- Whether `SectionButton` caches the `Button` component reference or uses `GetComponent<Button>()` at runtime
- Null guards on `PopupManager.Instance` and `App.Instance` (always apply per codebase conventions)
- Whether to add `[Tooltip]` on all `[SerializeField]` fields — yes, follow existing codebase pattern

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 1 Foundation (this phase builds on it directly)
- `.planning/phases/01-foundation/01-01-SUMMARY.md` — GroupData model, App.TryUnlock, OnUnlocked event, session restore
- `.planning/phases/01-foundation/01-02-SUMMARY.md` — ContentGate (LockedVisible/Hidden), PasswordPopup, SectionButton wires to PasswordPopup

### Codebase Conventions
- `.planning/codebase/CONVENTIONS.md` — #region block structure, naming conventions, [SerializeField] patterns
- `.planning/codebase/STRUCTURE.md` — Scripts/ folder organization, prefab locations

### Requirements
- `.planning/REQUIREMENTS.md` — INV-01→03 (Invite), INFO-01→07 (Home info), PERS-01→03 (personalized section)

### Existing Source Files (executor must read before modifying)
- `Assets/App/Scripts/Managers/GroupData.cs` — extend with hasApartment, apartmentName, apartmentAddress, apartmentNotes
- `Assets/App/Scripts/Popup/Popup.cs` — base class for all custom popup subclasses

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/App/Prefabs/HomeItem.prefab` + `HomeItem Variant.prefab` — candidate for section button prefabs (developer decides in Editor)
- `Assets/App/Prefabs/SideMenuItem.prefab` — for the burger menu side panel items
- `Assets/App/Prefabs/Popup/FullScreenPopup.prefab` — base prefab for per-section popups

### Established Patterns
- `ContentGate.cs` (Phase 1) — OnEnable/Disable event subscription pattern; `Refresh()` with null-check on `App.Instance` — **replicate exactly** in `GroupContentGate.cs`
- `SectionButton` is analogous to a "smart button" that knows about lock state — subscribe to `App.OnUnlocked` same as ContentGate
- `Popup` subclass pattern: override `OnShowing(object[] inData)` to populate UI from `App.Instance.CurrentGroup`

### Integration Points
- `PopupManager.Instance.Show(string id)` — how SectionButton opens popups
- `App.Instance.CurrentGroup` — read in `AlloggioPopup.OnShowing()` to branch UI
- `App.Instance.IsUnlocked` — SectionButton reads this to determine click behavior
- `App.OnUnlocked` (static event) — both SectionButton and GroupContentGate subscribe

</code_context>

<specifics>
## Specific Ideas

- **Italian locked hint text:** "inserisci il tuo codice per visualizzare" (exact wording from user)
- **Locked section visual pattern:** lock icon visible + hint text; sections appear in scroll in the same position, just with overlay/icon state changed — not moved or hidden
- **Burger menu:** opens existing side menu system (SideMenuItem.prefab), mirrors the home section list for quick access without navigating back to top
- **Alloggio popup variant:** groups WITHOUT apartment → generic B&B/hotel suggestions list; groups WITH apartment (`hasApartment = true`) → apartment name, address, notes from GroupData
- **Invite scene:** physical invitation images scrollable (no QR code shown), single "Entra" button at bottom → loads Home scene

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-wedding-content*
*Context gathered: 2026-04-17*
